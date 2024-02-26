namespace PlexWatch.Events;

public class StreamStartedEvent : BaseEvent
{
    public int SessionKey { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }
    public int RatingKey { get; set; }
    public string FullTitle { get; set; }
}
