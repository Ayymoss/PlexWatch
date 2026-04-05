namespace PlexWatch.Models.Plex;

public class Media
{
    public int? Bitrate { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool? Selected { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public string? VideoResolution { get; set; }
    public int? AudioChannels { get; set; }
    public string? Container { get; set; }
    public string? Protocol { get; set; }
    public IList<Part>? Part { get; set; }
}
