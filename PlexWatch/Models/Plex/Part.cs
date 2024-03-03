using System.Text.Json.Serialization;
using PlexWatch.Utilities;

namespace PlexWatch.Models.Plex;

public class Part
{
    public string? AudioProfile { get; set; }

    [JsonConverter(typeof(StringToIntConverter))]
    public int? Id { get; set; }

    public string? VideoProfile { get; set; }
    public int? Bitrate { get; set; }
    public string? Container { get; set; }
    public int? Duration { get; set; }
    public int? Height { get; set; }
    public string? Protocol { get; set; }
    public int? Width { get; set; }
    public string? Decision { get; set; }
    public bool? Selected { get; set; }
    public IList<Stream>? Stream { get; set; }
    public string? File { get; set; }
    public bool? Has64BitOffsets { get; set; }
    public string? Key { get; set; }
    public bool? OptimizedForStreaming { get; set; }
    public long? Size { get; set; }
}
