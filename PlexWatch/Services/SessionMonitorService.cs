using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexWatch.Configuration;
using PlexWatch.Enums;

namespace PlexWatch.Services;

public class SessionMonitorService : BackgroundService
{
    private readonly SessionContextFactory _contextFactory;
    private readonly RuleEvaluator _ruleEvaluator;
    private readonly SessionTerminator _sessionTerminator;
    private readonly DiscordNotifier _discordNotifier;
    private readonly ILogger<SessionMonitorService> _logger;
    private readonly SemaphoreSlim _checkLock = new(1, 1);
    private MonitoringSettings _settings;
    private CancellationTokenSource? _delayCts;

    public SessionMonitorService(
        SessionContextFactory contextFactory,
        RuleEvaluator ruleEvaluator,
        SessionTerminator sessionTerminator,
        DiscordNotifier discordNotifier,
        IOptionsMonitor<MonitoringSettings> optionsMonitor,
        ILogger<SessionMonitorService> logger)
    {
        _contextFactory = contextFactory;
        _ruleEvaluator = ruleEvaluator;
        _sessionTerminator = sessionTerminator;
        _discordNotifier = discordNotifier;
        _logger = logger;
        _settings = optionsMonitor.CurrentValue;
        optionsMonitor.OnChange(updated =>
        {
            _settings = updated;
            _logger.LogInformation("Monitoring settings reloaded");
        });
    }

    /// <summary>
    /// Cancels the current polling delay, causing an immediate session check.
    /// Called by the webhook endpoint when Plex reports a media event.
    /// </summary>
    public void TriggerCheck()
    {
        _logger.LogInformation("Session check triggered by webhook");
        _delayCts?.Cancel();
    }

    /// <summary>
    /// Polling loop that checks active Plex sessions on a configurable interval.
    /// Each cycle fetches sessions, evaluates rules, and terminates violating streams.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session monitor started, polling every {Interval}s", _settings.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session check");
            }

            try
            {
                _delayCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), _delayCts.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // Webhook triggered early check — continue to next iteration
            }
        }
    }

    private async Task CheckSessionsAsync(CancellationToken stoppingToken)
    {
        if (!await _checkLock.WaitAsync(0, stoppingToken)) return;

        try
        {
            var contexts = await _contextFactory.GetActiveSessionsAsync();

            foreach (var context in contexts)
            {
                var reason = _ruleEvaluator.Evaluate(context);

                _logger.LogInformation("Session -> {@SessionData}", new
                {
                    context.SessionId,
                    context.QualityProfile,
                    Terminate = reason,
                    context.UserTitle,
                    context.Title,
                    context.Player,
                    context.VideoDecision,
                    context.SourceVideoWidth,
                    context.StreamVideoWidth,
                });

                if (reason is TerminationReason.Ok) continue;

                await _sessionTerminator.TerminateAsync(context, reason);
                await _discordNotifier.NotifyTerminationAsync(context, reason);
            }
        }
        finally
        {
            _checkLock.Release();
        }
    }
}
