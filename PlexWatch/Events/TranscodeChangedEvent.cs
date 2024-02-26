namespace PlexWatch.Events;

public class TranscodeChangedEvent : BaseEvent
{
    public int SessionKey { get; set; }
}
