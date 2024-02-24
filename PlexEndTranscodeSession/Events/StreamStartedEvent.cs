namespace PlexEndTranscodeSession.Events;

public class StreamStartedEvent : BaseEvent
{
    public int Session { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }
    public int RatingKey { get; set; }
    public string FullTitle { get; set; }
}
