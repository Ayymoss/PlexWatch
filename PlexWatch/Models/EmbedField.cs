using System.Text.Json.Serialization;

namespace PlexWatch.Models;

public class EmbedField
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }

    [JsonPropertyName("inline")]
    public bool Inline { get; set; }
}
