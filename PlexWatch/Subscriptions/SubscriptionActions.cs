using Microsoft.Extensions.Logging;
using PlexWatch.Events;
using PlexWatch.Utilities;

namespace PlexWatch.Subscriptions;

public class SubscriptionActions(ILogger<SubscriptionActions> logger, TranscodeChecker transcodeChecker)
{
    public async Task OnStreamStartedEvent(StreamStartedEvent streamEvent, CancellationToken token)
    {
        logger.LogInformation("New stream started by {User} [{SessionKey}] ({Title})",
            streamEvent.UserName, streamEvent.SessionKey, streamEvent.FullTitle);
        await transcodeChecker.CheckForTranscode(token);
    }

    public async Task OnTranscodeChangedEvent(TranscodeChangedEvent transcodeEvent, CancellationToken token)
    {
        logger.LogInformation("Transcode detected [{SessionKey}]", transcodeEvent.SessionKey);
        await transcodeChecker.CheckForTranscode(token);
    }
}
