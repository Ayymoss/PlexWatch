using System.Text.Json.Serialization;

namespace PlexWatch.Models;

public class EmbedFooter
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }
}
