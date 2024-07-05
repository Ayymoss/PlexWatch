using PlexWatch.Enums;

namespace PlexWatch.Events;

public class BaseEvent
{
    public required string UserName { get; set; }
    public required string MediaTitle { get; set; }
    public required MediaType MediaType { get; set; }
    public required string RatingKey { get; set; }
}
