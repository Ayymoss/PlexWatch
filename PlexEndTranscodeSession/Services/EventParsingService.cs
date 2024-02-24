using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PlexEndTranscodeSession.Events;
using PlexEndTranscodeSession.Interfaces;

namespace PlexEndTranscodeSession.Services;

public partial class EventParsingService(FileWatcherService fileWatcherService, ILogger<EventParsingService> logger) : IDisposable
{
    [GeneratedRegex(@"^.+Session\s(\d+).+user\s(\d+)\s\(((\d|\w)+\*+\d)\).+ratingKey\s(\d+).+\((.+\s-\s.+)\)\.$")]
    private static partial Regex StreamStartedRegex();

    public async Task OnFileChanged(IEnumerable<string> newLines, CancellationToken token)
    {
        List<BaseEvent> events = [];
        foreach (var line in newLines)
        {
            var match = StreamStartedRegex().Match(line);
            if (!match.Success) continue;

            var streamStartedEvent = new StreamStartedEvent
            {
                Session = int.Parse(match.Groups[1].Value),
                UserId = int.Parse(match.Groups[2].Value),
                UserName = match.Groups[3].Value,
                RatingKey = int.Parse(match.Groups[5].Value),
                FullTitle = match.Groups[6].Value
            };
            events.Add(streamStartedEvent);
        }

        for (var i = 0; i < events.Count; i++)
        {
            logger.LogDebug("[{Index}/{Total}] Processing Event: {EventName}", i + 1, events.Count, events[i].GetType().Name);
            await IEventSubscriptions.InvokeEventAsync(events[i], token);
        }
    }

    public void Dispose()
    {
        fileWatcherService.OnFileChanged -= OnFileChanged;
    }
}
