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
    private readonly EventProcessingService _eventProcessingService;
    private CancellationTokenSource? _cancellationTokenSource;

    public AppEntry(SubscriptionActions subscriptionActions, FileWatcherService fileWatcherService, ILogger<AppEntry> logger,
        EventProcessingService eventProcessingService)
    {
        IEventSubscriptions.StreamStarted += subscriptionActions.OnStreamStartedEvent;
        IEventSubscriptions.TranscodeChanged += subscriptionActions.OnTranscodeChangedEvent;
        _fileWatcherService = fileWatcherService;
        _logger = logger;
        _eventProcessingService = eventProcessingService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting PlexWatch");
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        new Thread(() => _eventProcessingService.ProcessEvents(_cancellationTokenSource.Token))
            {Name = nameof(EventProcessingService)}.Start();
        _fileWatcherService.SetupFileWatcher(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping PlexWatch");
        _cancellationTokenSource?.Cancel(false);
        _logger.LogInformation("Stopped PlexWatch");
        return Task.CompletedTask;
    }
}
