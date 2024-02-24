using Microsoft.Extensions.Logging;
using PlexWatch.Interfaces;

namespace PlexWatch.Utilities;

public class TranscodeChecker(ILogger<TranscodeChecker> logger, ITautulliApi tautulliApi, Configuration config)
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task CheckForTranscode(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var response = await tautulliApi.GetActivityAsync(config.ApiKey);
            var sessions = response.ResponseRoot?.Data?.Sessions;
            if (sessions is null) return;

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
