namespace PlexWatch.Configuration;

public class PlexSettings
{
    public const string SectionName = "Plex";

    public string ServerUrl { get; set; } = "http://localhost:32400";
    public string Token { get; set; } = string.Empty;
}
