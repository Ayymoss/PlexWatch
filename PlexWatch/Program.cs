using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlexWatch.Configuration;
using PlexWatch.Enums;
using PlexWatch.Interfaces;
using PlexWatch.Models.Plex;
using PlexWatch.Services;
using PlexWatch.Utilities;
using Refit;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

namespace PlexWatch;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuration is loaded from appsettings.json, appsettings.{Environment}.json,
        // user-secrets (Development), and environment variables automatically by WebApplication.CreateBuilder.

        builder.Services.AddOpenApi();
        builder.Host.UseSerilog();

        RegisterConfiguration(builder);
        RegisterServices(builder);
        RegisterLogging(builder);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options => options.WithTitle("PlexWatch"));
        }

        MapEndpoints(app);

        await app.RunAsync();
        await Log.CloseAndFlushAsync();
    }

    private static void RegisterConfiguration(WebApplicationBuilder builder)
    {
        builder.Services.Configure<PlexSettings>(builder.Configuration.GetSection(PlexSettings.SectionName));
        builder.Services.Configure<DiscordSettings>(builder.Configuration.GetSection(DiscordSettings.SectionName));
        builder.Services.Configure<MonitoringSettings>(builder.Configuration.GetSection(MonitoringSettings.SectionName));
    }

    private static void RegisterServices(WebApplicationBuilder builder)
    {
        var plexSettings = builder.Configuration.GetSection(PlexSettings.SectionName).Get<PlexSettings>() ?? new PlexSettings();
        var discordSettings = builder.Configuration.GetSection(DiscordSettings.SectionName).Get<DiscordSettings>() ?? new DiscordSettings();

        builder.Services.AddSingleton<SessionSnapshotService>();
        builder.Services.AddSingleton<SessionContextFactory>();
        builder.Services.AddSingleton<RuleEvaluator>();
        builder.Services.AddSingleton<SessionTerminator>();
        builder.Services.AddSingleton<DiscordNotifier>();
        builder.Services.AddSingleton<SessionMonitorService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<SessionMonitorService>());

        builder.Services.AddRefitClient<IPlexApi>().ConfigureHttpClient(c =>
        {
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            c.DefaultRequestHeaders.Add("X-Plex-Token", plexSettings.Token);
            c.BaseAddress = new Uri(plexSettings.ServerUrl);
        });

        builder.Services.AddRefitClient<IDiscord>().ConfigureHttpClient(c =>
            c.BaseAddress = new Uri(discordSettings.WebhookUrl));
    }

    private static void RegisterLogging(WebApplicationBuilder builder)
    {
        var debug = builder.Configuration.GetValue<bool>("Debug");

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

        if (debug)
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

    private static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok());

        app.MapPost("/plex-webhook", async (HttpRequest request, SessionMonitorService monitor) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest("Invalid Content-Type");

            try
            {
                var form = await request.ReadFormAsync();
                var payload = form["payload"].ToString();
                var webhook = JsonSerializer.Deserialize<WebhookRoot>(payload, JsonConverters.JsonOptions);
                if (webhook is null) return Results.BadRequest("Invalid payload");

                if (!webhook.Event.ToString().Contains("Media", StringComparison.OrdinalIgnoreCase))
                    return Results.Ok();
                if (webhook.Metadata.Type is not (MediaType.Episode or MediaType.Movie))
                    return Results.Ok();

                Log.Information("[WEBHOOK] {Event} by {User} - {Title}",
                    webhook.Event, webhook.Account.Title, webhook.Metadata.Title);

                monitor.TriggerCheck();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error processing Plex webhook");
                return Results.BadRequest("Error processing webhook");
            }

            return Results.Ok();
        });
    }
}
