using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlexWatch.Interfaces;
using PlexWatch.Services;
using PlexWatch.Subscriptions;

namespace PlexWatch;

public class AppEntry : IHostedService
{
    private readonly ILogger<AppEntry> _logger;
    private readonly EventProcessingService _eventProcessingService;
    private CancellationTokenSource? _cancellationTokenSource;

    public AppEntry(SubscriptionActions subscriptionActions, ILogger<AppEntry> logger, EventProcessingService eventProcessingService)
    {
        IEventSubscriptions.MediaPlayed += subscriptionActions.OnMediaPlayed;
        IEventSubscriptions.MediaResumed += subscriptionActions.OnMediaResumed;
        IEventSubscriptions.MediaPaused += subscriptionActions.OnMediaPaused;
        IEventSubscriptions.MediaStopped += subscriptionActions.OnMediaStopped;
        _logger = logger;
        _eventProcessingService = eventProcessingService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting PlexWatch");
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        new Thread(() => _eventProcessingService.ProcessEvents(_cancellationTokenSource.Token))
            { Name = nameof(EventProcessingService) }.Start();
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
