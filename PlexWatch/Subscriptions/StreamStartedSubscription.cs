using Microsoft.Extensions.Logging;
using PlexWatch.Events;
using PlexWatch.Utilities;

namespace PlexWatch.Subscriptions;

public class StreamStartedSubscription(ILogger<StreamStartedSubscription> logger, TranscodeChecker transcodeChecker)
{
    public async Task OnStreamStartedEvent(StreamStartedEvent streamEvent, CancellationToken token)
    {
        logger.LogInformation("New stream started by {User} ({Title})", streamEvent.UserName, streamEvent.FullTitle);
        await transcodeChecker.CheckForTranscode(token);
    }
}
