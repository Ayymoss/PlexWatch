using System.Text.Json.Serialization;

namespace PlexWatch.Models;

public class DiscordWebhookPayload
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("embeds")]
    public required Embed[] Embeds { get; set; }
}
