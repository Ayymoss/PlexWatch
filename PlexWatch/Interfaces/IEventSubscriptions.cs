using PlexWatch.Events;
using PlexWatch.Utilities;

namespace PlexWatch.Interfaces;

public interface IEventSubscriptions
{
    static event Func<MediaPlayEvent, CancellationToken, Task>? MediaPlayed;
    static event Func<MediaResumeEvent, CancellationToken, Task>? MediaResumed;
    static event Func<MediaPauseEvent, CancellationToken, Task>? MediaPaused;
    static event Func<MediaStopEvent, CancellationToken, Task>? MediaStopped;

    static Task InvokeEventAsync<TBaseEvent>(TBaseEvent baseEvent, CancellationToken token) where TBaseEvent : BaseEvent
    {
        return baseEvent switch
        {
            // @formatter:off
            MediaPlayEvent mediaPlayEvent => MediaPlayed?.InvokeAsync(mediaPlayEvent, token) ?? Task.CompletedTask,
            MediaResumeEvent mediaResumeEvent => MediaResumed?.InvokeAsync(mediaResumeEvent, token) ?? Task.CompletedTask,
            MediaPauseEvent mediaPauseEvent => MediaPaused?.InvokeAsync(mediaPauseEvent, token) ?? Task.CompletedTask,
            MediaStopEvent mediaStopEvent => MediaStopped?.InvokeAsync(mediaStopEvent, token) ?? Task.CompletedTask,
            _ => Task.CompletedTask
            // @formatter:on
        };
    }
}
