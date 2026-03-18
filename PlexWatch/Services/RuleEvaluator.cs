using Microsoft.Extensions.Options;
using PlexWatch.Configuration;
using PlexWatch.Enums;
using PlexWatch.Models;

namespace PlexWatch.Services;

public class RuleEvaluator
{
    private MonitoringSettings _settings;

    public RuleEvaluator(IOptionsMonitor<MonitoringSettings> optionsMonitor)
    {
        _settings = optionsMonitor.CurrentValue;
        optionsMonitor.OnChange(updated => _settings = updated);
    }

    /// <summary>
    /// Evaluates a session against all configured rules and returns the first violation found.
    /// Rules are checked in priority order: blocked client type, quality settings, resolution mismatch, then device blocks.
    /// Returns <see cref="TerminationReason.Ok"/> if the session passes all checks.
    /// </summary>
    public TerminationReason Evaluate(SessionContext session)
    {
        if (IsPlexWeb(session.Player))
            return TerminationReason.IncorrectClient;

        if (!session.QualityProfile.Equals("Original", StringComparison.OrdinalIgnoreCase))
            return TerminationReason.RemoteQualityUnset;

        if (Math.Abs(session.SourceVideoWidth - session.StreamVideoWidth) > 0.1 * session.SourceVideoWidth)
            return TerminationReason.StreamWidthMismatch;

        if (IsBlockedClient(session.UserTitle, session.Player))
            return TerminationReason.BlockedClient;

        return TerminationReason.Ok;
    }

    private static bool IsPlexWeb(string? player) =>
        !string.IsNullOrWhiteSpace(player) && player.Contains("Plex Web", StringComparison.OrdinalIgnoreCase);

    private bool IsBlockedClient(string? user, string? player)
    {
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(player)) return false;
        if (_settings.BlockedDeviceNames.Count is 0) return false;

        if (!_settings.BlockedDeviceNames.TryGetValue(user, out var players)) return false;
        return players.FirstOrDefault() == "*" || players.Contains(player);
    }
}
