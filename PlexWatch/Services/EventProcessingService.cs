using Microsoft.Extensions.Logging;
using PlexWatch.Events;
using PlexWatch.Interfaces;
using PlexWatch.Utilities;

namespace PlexWatch.Services;

public class EventProcessingService(ITautulliApi tautulliApi, Configuration config, ILogger<EventProcessingService> logger)
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task OnStreamStartedEvent(StreamStartedEvent streamEvent, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var response = await tautulliApi.GetActivityAsync(config.ApiKey);
            var sessionData = response.ResponseRoot?.Data;
            var sessions = sessionData?.Sessions;
            if (sessions is null) return;

            logger.LogInformation("New stream started by {User} ({Title}) - Open Streams: {StreamCount}",
                streamEvent.UserName, streamEvent.FullTitle, sessionData?.StreamCount);

            foreach (var session in sessions)
            {
                if (session.FullTitle.Contains("preroll", StringComparison.CurrentCultureIgnoreCase)) continue;
                if (session.QualityProfile.Equals("Original", StringComparison.CurrentCulture)) continue;

                logger.LogInformation(
                    "Terminating ({SessionId} - {User}) {Title} [Quality: {Quality}, Video Decision: {VideoDecision}, Audio Decision: {AudioDecision}]",
                    session.SessionId, session.User, session.FullTitle, session.QualityProfile, session.VideoDecision,
                    session.AudioDecision);
                await tautulliApi.TerminateSessionAsync(config.ApiKey, session.SessionKey, session.SessionId,
                    "[TRANSCODE] Adjust Plex's Remote Quality to Original or Maximum");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error executing scheduled action");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
