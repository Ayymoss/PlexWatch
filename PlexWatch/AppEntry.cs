using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlexWatch.Interfaces;
using PlexWatch.Services;

namespace PlexWatch;

public class AppEntry(
    EventProcessingService eventProcessingService,
    EventParsingService eventParsingService,
    FileWatcherService fileWatcherService,
    ILogger<AppEntry> logger)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting PlexEndTranscodeSession");
        fileWatcherService.SetupFileWatcher(cancellationToken);
        fileWatcherService.OnFileChanged += eventParsingService.OnFileChanged;
        IEventSubscriptions.StreamStarted += eventProcessingService.OnStreamStartedEvent;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
