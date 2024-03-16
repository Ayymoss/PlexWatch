using Microsoft.Extensions.Logging;
using PlexWatch.Enums;
using PlexWatch.Events;
using PlexWatch.Models.Plex;

namespace PlexWatch.Services;

public class EventParsingService(EventProcessingService eventProcessingService, ILogger<EventParsingService> logger)
{
    public void OnWebHookReceived(WebhookRoot plex)
    {
        try
        {
            BaseEvent? baseEvent = null;
            if (plex.Event.ToString().Contains("media", StringComparison.OrdinalIgnoreCase))
            {
                if (plex.Metadata.Title?.Contains("preroll", StringComparison.OrdinalIgnoreCase) ?? false) return;

                // Episodes have a GrandparentTitle that we want to include in the event title
                var title = plex.Metadata.Type is MediaType.Episode
                    ? $"{plex.Metadata.GrandparentTitle}: {plex.Metadata.Title}"
                    : plex.Metadata.Title;

                baseEvent = plex.Event switch
                {
                    PlexWebhookEventType.MediaPlay => new MediaPlayEvent
                    {
                        UserName = plex.Account.Title,
                        MediaTitle = title,
                        MediaType = plex.Metadata.Type,
                        RatingKey = plex.Metadata.RatingKey,
                    },
                    PlexWebhookEventType.MediaPause => new MediaPauseEvent
                    {
                        UserName = plex.Account.Title,
                        MediaTitle = title,
                        MediaType = plex.Metadata.Type,
                        RatingKey = plex.Metadata.RatingKey,
                    },
                    PlexWebhookEventType.MediaResume => new MediaResumeEvent
                    {
                        UserName = plex.Account.Title,
                        MediaTitle = title,
                        MediaType = plex.Metadata.Type,
                        RatingKey = plex.Metadata.RatingKey,
                    },
                    PlexWebhookEventType.MediaStop => new MediaStopEvent
                    {
                        UserName = plex.Account.Title,
                        MediaTitle = title,
                        MediaType = plex.Metadata.Type,
                        RatingKey = plex.Metadata.RatingKey,
                    },
                    _ => null
                };
            }

            if (baseEvent is null) return;
            eventProcessingService.QueueEvent(baseEvent);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error parsing [Plex Webhook: {@PlexWebhook}]", plex);
        }
    }
}
