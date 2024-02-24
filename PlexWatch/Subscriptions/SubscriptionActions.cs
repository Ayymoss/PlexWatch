using Microsoft.Extensions.Logging;
using PlexWatch.Events;
using PlexWatch.Utilities;

namespace PlexWatch.Subscriptions;

public class SubscriptionActions(ILogger<SubscriptionActions> logger, TranscodeChecker transcodeChecker)
{
    public async Task OnStreamStartedEvent(StreamStartedEvent streamEvent, CancellationToken token)
    {
        logger.LogInformation("New stream started by {User} [{Session}] ({Title})",
            streamEvent.UserName, streamEvent.Session, streamEvent.FullTitle);
        await transcodeChecker.CheckForTranscode(token);
    }

    public async Task OnTranscodeChangedEvent(TranscodeChangedEvent transcodeEvent, CancellationToken token)
    {
        logger.LogInformation("Transcode detected by {Session}", transcodeEvent.Session);
        await transcodeChecker.CheckForTranscode(token);
    }
}
