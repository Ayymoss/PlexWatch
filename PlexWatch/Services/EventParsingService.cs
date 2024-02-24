using System.Text.RegularExpressions;
using PlexWatch.Events;

namespace PlexWatch.Services;

public partial class EventParsingService(EventProcessingService eventProcessingService)
{
    [GeneratedRegex(@"^.+Session\s(\d+).+user\s(\d+)\s\(((?:\d|\w)+\*+\d)\).+ratingKey\s(\d+)\s\((.+)\)\.$")]
    private static partial Regex StreamStartedRegex();

    [GeneratedRegex(@"^.+Session\s(\d+)\shas\schanged\stranscode\sdecision\.$")]
    private static partial Regex TranscodeChangedRegex();

    public void OnFileChanged(IEnumerable<string> newLines)
    {
        List<BaseEvent> events = [];
        foreach (var line in newLines)
        {
            var transcodeChanged = TranscodeChangedRegex().Match(line);
            var streamStart = StreamStartedRegex().Match(line);

            if (streamStart.Success)
            {
                if (streamStart.Groups[5].Value.Contains("preroll", StringComparison.CurrentCultureIgnoreCase)) continue;

                var streamStartedEvent = new StreamStartedEvent
                {
                    Session = int.Parse(streamStart.Groups[1].Value),
                    UserId = int.Parse(streamStart.Groups[2].Value),
                    UserName = streamStart.Groups[3].Value,
                    RatingKey = int.Parse(streamStart.Groups[4].Value),
                    FullTitle = streamStart.Groups[5].Value
                };
                events.Add(streamStartedEvent);
            }

            if (transcodeChanged.Success)
            {
                var streamStartedEvent = new TranscodeChangedEvent
                {
                    Session = int.Parse(transcodeChanged.Groups[1].Value)
                };
                events.Add(streamStartedEvent);
            }
        }

        eventProcessingService.QueueEvents(events);
    }
}
