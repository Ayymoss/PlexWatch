using PlexWatch.Events;
using PlexWatch.Utilities;

namespace PlexWatch.Interfaces;

public interface IEventSubscriptions
{
    static event Func<StreamStartedEvent, CancellationToken, Task>? StreamStarted;
    static event Func<TranscodeChangedEvent, CancellationToken, Task>? TranscodeChanged;

    static Task InvokeEventAsync<TBaseEvent>(TBaseEvent baseEvent, CancellationToken token) where TBaseEvent : BaseEvent
    {
        return baseEvent switch
        {
            // @formatter:off
            StreamStartedEvent streamStartedEvent => StreamStarted?.InvokeAsync(streamStartedEvent, token) ?? Task.CompletedTask,
            TranscodeChangedEvent transcodeChangedEvent => TranscodeChanged?.InvokeAsync(transcodeChangedEvent, token) ?? Task.CompletedTask,
            _ => Task.CompletedTask
            // @formatter:on
        };
    }
}
