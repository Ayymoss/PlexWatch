using System.Text.Json.Serialization;
using PlexWatch.Utilities;

namespace PlexWatch.Models.Plex;

public class Stream
{
    public int? Bitrate { get; set; }
    public string? Codec { get; set; }
    public bool? Default { get; set; }
    public string? DisplayTitle { get; set; }
    public string? ExtendedDisplayTitle { get; set; }
    public double? FrameRate { get; set; }
    public int? Height { get; set; }

    [JsonConverter(typeof(StringToIntConverter))]
    public int? Id { get; set; }

    public string? Language { get; set; }
    public string? LanguageCode { get; set; }
    public string? LanguageTag { get; set; }
    public int? StreamType { get; set; }
    public int? Width { get; set; }
    public string? Decision { get; set; }
    public string? Location { get; set; }
    public string? BitrateMode { get; set; }
    public int? Channels { get; set; }
    public bool? Selected { get; set; }
    public int? BitDepth { get; set; }
    public string? ChromaLocation { get; set; }
    public string? ChromaSubsampling { get; set; }
    public string? CodecId { get; set; }
    public int? CodedHeight { get; set; }
    public int? CodedWidth { get; set; }
    public string? ColorRange { get; set; }
    public int? Index { get; set; }
    public int? Level { get; set; }
    public string? Profile { get; set; }
    public int? RefFrames { get; set; }
    public string? StreamIdentifier { get; set; }
    public string? AudioChannelLayout { get; set; }
    public int? SamplingRate { get; set; }
}
