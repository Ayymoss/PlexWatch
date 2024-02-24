using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlexEndTranscodeSession.Interfaces;
using PlexEndTranscodeSession.Services;
using PlexEndTranscodeSession.Utilities;
using Refit;
using Serilog;
using Serilog.Events;

namespace PlexEndTranscodeSession;

public static class Program
{
    public static async Task Main()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.UseSerilog();
        builder.ConfigureServices(RegisterDependencies);
        var app = builder.Build();
        await app.RunAsync();
    }

    private static void RegisterDependencies(HostBuilderContext builder, IServiceCollection service)
    {
        var configuration = SetupConfiguration.ReadConfiguration();
        RegisterLogging();

        service.AddSingleton(configuration);
        service.AddSingleton<FileWatcherService>();
        service.AddSingleton<EventParsingService>();
        service.AddSingleton<EventProcessingService>();
        service.AddHostedService<AppEntry>();
        service.AddRefitClient<ITautulliApi>().ConfigureHttpClient(c => c.BaseAddress = new Uri("http://10.10.1.7:8181"));
        service.AddSerilog();
    }

    private static void RegisterLogging()
    {
        if (!Directory.Exists(Path.Join(AppContext.BaseDirectory, "_Log")))
            Directory.CreateDirectory(Path.Join(AppContext.BaseDirectory, "_Log"));

        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Information()
            .MinimumLevel.Override("PlexEndTranscodeSession", LogEventLevel.Debug)
#else
            .MinimumLevel.Warning()
            .MinimumLevel.Override("PlexEndTranscodeSession", LogEventLevel.Information)
#endif
            .Enrich.FromLogContext()
            .Enrich.With<ShortSourceContextEnricher>()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Join(AppContext.BaseDirectory, "_Log", "pks-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
