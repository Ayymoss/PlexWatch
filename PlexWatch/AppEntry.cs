using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlexWatch.Interfaces;
using PlexWatch.Services;
using PlexWatch.Subscriptions;

namespace PlexWatch;

public class AppEntry : IHostedService
{
    private readonly FileWatcherService _fileWatcherService;
    private readonly ILogger<AppEntry> _logger;

    public AppEntry(StreamStartedSubscription streamStartedSubscription,TranscodeChangedSubscription transcodeChangedSubscription, FileWatcherService fileWatcherService, ILogger<AppEntry> logger)
    {
        IEventSubscriptions.StreamStarted += streamStartedSubscription.OnStreamStartedEvent;
        IEventSubscriptions.TranscodeChanged += transcodeChangedSubscription.OnTranscodeChangedEvent;
        _fileWatcherService = fileWatcherService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting PlexEndTranscodeSession");
        _fileWatcherService.SetupFileWatcher(cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
