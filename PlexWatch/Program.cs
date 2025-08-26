using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PlexWatch.Interfaces;
using PlexWatch.Models.Plex;
using PlexWatch.Services;
using PlexWatch.Subscriptions;
using PlexWatch.Utilities;
using Refit;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

namespace PlexWatch;

public static class Program
{
    public static async Task Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.SetBasePath(Path.Join(Directory.GetCurrentDirectory(), "_Configuration"))
            .AddJsonFile("Configuration.json", false, true);

        builder.Services.AddOpenApi();

        builder.Host.UseSerilog();
        builder.Host.ConfigureServices(RegisterDependencies);

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options => { options.WithTitle("Plex Watch"); });
        }
        else
        {
            var configuration = app.Services.GetRequiredService<IOptionsMonitor<Configuration>>().CurrentValue;
            app.Urls.Add(configuration.BindAddress);
        }

        MapEndpoints(app);

        // Manually resolve services
        app.Services.GetRequiredService<EventParsingService>();

        await app.RunAsync();
        await Log.CloseAndFlushAsync();
    }

    private static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok());

        app.MapPost("/plex-webhook", async (HttpRequest request, EventParsingService eventParsingService) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest("Invalid Content-Type");

            try
            {
                var form = await request.ReadFormAsync();
                var payload = form["payload"].ToString();
                var plex = JsonSerializer.Deserialize<WebhookRoot>(payload, JsonConverters.JsonOptions);
                if (plex is null) return Results.BadRequest("Invalid payload");
                eventParsingService.OnWebHookReceived(plex);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error ingesting Plex Webhook");
                return Results.BadRequest("Error ingesting Plex Webhook");
            }

            return Results.Ok();
        });
    }

    private static void RegisterDependencies(HostBuilderContext builder, IServiceCollection service)
    {
        service.Configure<Configuration>(builder.Configuration);

        var configuration = builder.Configuration.Get<Configuration>() ?? new Configuration();
        RegisterLogging(configuration);

        // Services
        service.AddSingleton<EventParsingService>();
        service.AddSingleton<EventProcessingService>();
        service.AddSingleton<TranscodeChecker>();
        service.AddSingleton<DiscordWebhookService>();

        // Subscriptions
        service.AddSingleton<SubscriptionActions>();

        // Core
        service.AddHostedService<AppEntry>();
        service.AddRefitClient<IPlexApi>().ConfigureHttpClient(c =>
        {
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            c.DefaultRequestHeaders.Add("X-Plex-Token", configuration.PlexToken);
            c.BaseAddress = new Uri("http://10.10.1.6:32400");
        });
        service.AddRefitClient<IDiscord>().ConfigureHttpClient(c => c.BaseAddress = new Uri(configuration.DiscordWebhook));
    }

    private static void RegisterLogging(Configuration configuration)
    {
        if (!Directory.Exists(Path.Join(AppContext.BaseDirectory, "_Log")))
            Directory.CreateDirectory(Path.Join(AppContext.BaseDirectory, "_Log"));

        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.With<ShortSourceContextEnricher>()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Join(AppContext.BaseDirectory, "_Log", "PlexWatch-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{ShortSourceContext}] {Message:lj}{NewLine}{Exception}");

        if (configuration.Debug)
        {
            loggerConfig.MinimumLevel.Information();
            loggerConfig.MinimumLevel.Override("PlexWatch", LogEventLevel.Debug);
        }
        else
        {
            loggerConfig.MinimumLevel.Warning();
            loggerConfig.MinimumLevel.Override("PlexWatch", LogEventLevel.Information);
        }

        Log.Logger = loggerConfig.CreateLogger();
    }
}
