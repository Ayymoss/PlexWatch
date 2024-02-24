namespace PlexWatch.Events;

public class TranscodeChangedEvent : BaseEvent
{
    public int Session { get; set; }
}
