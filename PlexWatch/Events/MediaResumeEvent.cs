
using PlexWatch.Enums;

namespace PlexWatch.Events;

public class MediaResumeEvent : BaseEvent
{
    public string UserName { get; set; }
    public string MediaTitle { get; set; }
    public MediaType MediaType { get; set; }
    public string RatingKey { get; set; }
}
