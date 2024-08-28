namespace PlexWatch.Models.Plex;

public class Media
{
    public int? Bitrate { get; set; }
    public int? Width { get; set; }
    public IList<Part>? Part { get; set; }
}
