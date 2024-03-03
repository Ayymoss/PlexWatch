namespace PlexWatch.Models.Plex;

public class MediaContainer
{
    public int? Size { get; set; }
    public IList<Metadata>? Metadata { get; set; }
}
