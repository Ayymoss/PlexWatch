namespace PlexWatch.Models.Plex;

public class Stream
{
    public int? Bitrate { get; set; }
    public string? Codec { get; set; }
    public string? Decision { get; set; }
    public string? Location { get; set; }
    public int? StreamType { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Channels { get; set; }
    public string? DisplayTitle { get; set; }
}
