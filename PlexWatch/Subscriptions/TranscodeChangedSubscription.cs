using Microsoft.Extensions.Logging;
using PlexWatch.Events;
using PlexWatch.Utilities;

namespace PlexWatch.Subscriptions;

public class TranscodeChangedSubscription(ILogger<StreamStartedSubscription> logger, TranscodeChecker transcodeChecker)
{
    public async Task OnTranscodeChangedEvent(TranscodeChangedEvent transcodeEvent, CancellationToken token)
    {
        logger.LogInformation("Transcode detected by {Session}", transcodeEvent.Session);
        await transcodeChecker.CheckForTranscode(token);
    }
}
