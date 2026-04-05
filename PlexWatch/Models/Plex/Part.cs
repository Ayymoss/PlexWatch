namespace PlexWatch.Models.Plex;

public class Part
{
    public string? Decision { get; set; }
    public string? Protocol { get; set; }
    public string? Container { get; set; }
    public string? File { get; set; }
    public IList<Stream>? Stream { get; set; }
}
