using PlexEndTranscodeSession.Events;
using PlexEndTranscodeSession.Utilities;

namespace PlexEndTranscodeSession.Interfaces;

public interface IEventSubscriptions
{
    static event Func<StreamStartedEvent, CancellationToken, Task>? StreamStarted;

    static Task InvokeEventAsync<TBaseEvent>(TBaseEvent baseEvent, CancellationToken token) where TBaseEvent : BaseEvent
    {
        return baseEvent switch
        {
            StreamStartedEvent streamStartedEvent => StreamStarted?.InvokeAsync(streamStartedEvent, token) ?? Task.CompletedTask,
            _ => Task.CompletedTask
        };
    }
}
