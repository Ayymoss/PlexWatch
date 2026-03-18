using Microsoft.Extensions.Logging;
using PlexWatch.Enums;
using PlexWatch.Interfaces;
using PlexWatch.Models;

namespace PlexWatch.Services;

public class SessionTerminator(IPlexApi plexApi, ILogger<SessionTerminator> logger)
{
    /// <summary>
    /// Terminates a Plex session via the API with a user-friendly message explaining
    /// why the stream was stopped and how to resolve the issue.
    /// Note: some Plex clients replace newlines with spaces, so messages must read well either way.
    /// </summary>
    public async Task TerminateAsync(SessionContext session, TerminationReason reason)
    {
        var userMessage = GetUserMessage(reason);

        logger.LogWarning("Terminating session {SessionId} for {User} - {Reason}",
            session.SessionId, session.UserTitle, reason);

        await plexApi.TerminateSessionAsync(session.SessionId, userMessage);
    }

    private static string GetUserMessage(TerminationReason reason) => reason switch
    {
        TerminationReason.StreamWidthMismatch =>
            "Your stream was stopped because your quality settings are reducing the video quality." +
            "\nPlease go to Settings > Quality and set 'Remote Quality' to 'Original'.",
        TerminationReason.RemoteQualityUnset =>
            "Your stream was stopped because 'Remote Quality' is not set to 'Original'." +
            "\nPlease go to Settings > Quality and change it to 'Original'.",
        TerminationReason.IncorrectClient =>
            "Plex Web is not allowed on this server." +
            "\nPlease use the Plex Desktop app instead. You can download it from plex.tv",
        TerminationReason.BlockedClient =>
            "This device is not allowed to stream from this server." +
            "\nPlease contact the server owner if you think this is a mistake.",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Invalid termination reason")
    };
}
