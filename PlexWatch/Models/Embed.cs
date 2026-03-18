using System.Text.Json.Serialization;

namespace PlexWatch.Models;

public class Embed
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("color")]
    public int? Color { get; set; }

    [JsonPropertyName("fields")]
    public EmbedField[]? Fields { get; set; }

    [JsonPropertyName("footer")]
    public EmbedFooter? Footer { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}
