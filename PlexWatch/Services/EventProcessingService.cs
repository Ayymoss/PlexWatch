using Microsoft.Extensions.Logging;
using PlexWatch.Events;
using PlexWatch.Interfaces;

namespace PlexWatch.Services;

public class EventProcessingService(ILogger<EventProcessingService> logger)
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task ProcessEvents(List<BaseEvent> events, CancellationToken token)
    {
        // TODO: Fix. Events are getting stuck here whilst existing are being processed
        // Events stacking could be balloon
        await _semaphore.WaitAsync(token);
        try
        {
            for (var i = 0; i < events.Count; i++)
            {
                logger.LogDebug("[{Index}/{Total}] Processing Event: {EventName}", i + 1, events.Count, events[i].GetType().Name);
                await IEventSubscriptions.InvokeEventAsync(events[i], token);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
