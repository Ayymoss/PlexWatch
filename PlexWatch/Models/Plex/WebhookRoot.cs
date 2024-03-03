using System.Text.Json.Serialization;
using PlexWatch.Enums;
using PlexWatch.Utilities;

namespace PlexWatch.Models.Plex;

public class WebhookRoot
{
    [JsonConverter(typeof(WebhookToEventEnumConverter))] public PlexWebhookEventType Event { get; set; }
    public bool User { get; set; }
    public bool Owner { get; set; }
    public Account Account { get; set; }
    public Server Server { get; set; }

    public Player Player { get; set; }
    public Metadata Metadata { get; set; }
}
