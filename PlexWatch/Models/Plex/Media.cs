using System.Text.Json.Serialization;
using PlexWatch.Utilities;

namespace PlexWatch.Models.Plex;

public class Media
{
    public string? AudioProfile { get; set; }

    [JsonConverter(typeof(StringToIntConverter))]
    public int? Id { get; set; }

    public string? Origin { get; set; }
    public string? VideoProfile { get; set; }
    public int? AudioChannels { get; set; }
    public string? AudioCodec { get; set; }
    public int? Bitrate { get; set; }
    public string? Container { get; set; }
    public int? Duration { get; set; }
    public int? Height { get; set; }
    public string? Protocol { get; set; }
    public string? VideoCodec { get; set; }
    public string? VideoFrameRate { get; set; }
    public string? VideoResolution { get; set; }
    public int? Width { get; set; }
    public bool? Selected { get; set; }
    public IList<Part>? Part { get; set; }

    [JsonConverter(typeof(StringToFloatConverter))]
    public float? AspectRatio { get; set; }

    public bool? Has64BitOffsets { get; set; }
    [JsonConverter(typeof(IntToBoolConverter))]
    public bool? OptimizedForStreaming { get; set; }
}
