using Microsoft.Extensions.Logging;
using PlexWatch.Events;
using PlexWatch.Utilities;

namespace PlexWatch.Subscriptions;

public class SubscriptionActions(ILogger<SubscriptionActions> logger, TranscodeChecker transcodeChecker)
{
    public async Task OnMediaPlayed(MediaPlayEvent mediaPlayEvent, CancellationToken token)
    {
        logger.LogInformation("[PLAYED] {MediaType} played by {User} [{SessionKey}] ({Title})",
            mediaPlayEvent.MediaType.ToString(), mediaPlayEvent.UserName, mediaPlayEvent.RatingKey, mediaPlayEvent.MediaTitle);
        await transcodeChecker.CheckForTranscode(token);
    }

    public async Task OnMediaResumed(MediaResumeEvent mediaResumeEvent, CancellationToken token)
    {
        logger.LogInformation("[RESUMED] {MediaType} resumed by {User} ([{SessionKey}] {Title})",
            mediaResumeEvent.MediaType.ToString(), mediaResumeEvent.UserName, mediaResumeEvent.RatingKey, mediaResumeEvent.MediaTitle);
        await transcodeChecker.CheckForTranscode(token);
    }

    public async Task OnMediaPaused(MediaPauseEvent mediaResumeEvent, CancellationToken token)
    {
        logger.LogInformation("[PAUSED] {MediaType} paused by {User} ([{SessionKey}] {Title})",
            mediaResumeEvent.MediaType.ToString(), mediaResumeEvent.UserName, mediaResumeEvent.RatingKey, mediaResumeEvent.MediaTitle);
        await transcodeChecker.CheckForTranscode(token);
    }

    public async Task OnMediaStopped(MediaStopEvent mediaResumeEvent, CancellationToken token)
    {
        logger.LogInformation("[STOPPED] {MediaType} paused by {User} ([{SessionKey}] {Title})",
            mediaResumeEvent.MediaType.ToString(), mediaResumeEvent.UserName, mediaResumeEvent.RatingKey, mediaResumeEvent.MediaTitle);
        await transcodeChecker.CheckForTranscode(token);
    }
}
