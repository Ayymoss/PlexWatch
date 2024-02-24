using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PlexWatch.Events;
using PlexWatch.Interfaces;

namespace PlexWatch.Services;

/// <summary>
/// Heavily inspired by @RaidMax's event processing implementation https://github.com/RaidMax/IW4M-Admin
/// </summary>
public class EventProcessingService(ILogger<EventProcessingService> logger)
{
    private const int MaxCurrentEvents = 20;
    private readonly ManualResetEventSlim _onEventReady = new(false);
    private readonly SemaphoreSlim _onProcessingEvents = new(MaxCurrentEvents, MaxCurrentEvents);
    private readonly ConcurrentQueue<BaseEvent> _events = [];
    private CancellationToken _token;
    private int _activeTasks;

    public void QueueEvent(BaseEvent baseEvent)
    {
        _events.Enqueue(baseEvent);
        _onEventReady.Set();
    }

    public void QueueEvents(List<BaseEvent> baseEvents)
    {
        foreach (var baseEvent in baseEvents) _events.Enqueue(baseEvent);
        _onEventReady.Set();
    }

    public void ProcessEvents(CancellationToken token)
    {
        _token = token;

        while (!token.IsCancellationRequested)
        {
            _onEventReady.Reset();
            try
            {
                _onProcessingEvents.Wait(_token);

                if (!_events.TryDequeue(out var coreEvent))
                {
                    if (_onProcessingEvents.CurrentCount < MaxCurrentEvents) _onProcessingEvents.Release(1);

                    _onEventReady.Wait(_token);
                    continue;
                }

                logger.LogInformation("Start processing event {Name} {SemaphoreCount} - {QueuedTasks}",
                    coreEvent.GetType().Name, _onProcessingEvents.CurrentCount, _events.Count);

                _ = Task.Factory.StartNew(async () =>
                {
                    Interlocked.Increment(ref _activeTasks);
                    logger.LogDebug("Active Tasks = {TaskCount}", _activeTasks);
                    try
                    {
                        await IEventSubscriptions.InvokeEventAsync(coreEvent, token);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error processing {Event} - {@EventState}", coreEvent.GetType().Name, coreEvent);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _activeTasks);
                        if (_onProcessingEvents.CurrentCount < MaxCurrentEvents) _onProcessingEvents.Release(1);
                    }
                }, token);
            }
            catch (OperationCanceledException e)
            {
                if (!token.IsCancellationRequested)
                {
                    logger.LogError(e, "Unexpected cancel error processing event");
                    continue;
                }

                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing event");
            }
        }
    }
}
