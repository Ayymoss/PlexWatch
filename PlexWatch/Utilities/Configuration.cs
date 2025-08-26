namespace PlexWatch.Utilities;

public class Configuration
{
    public string? PlexToken { get; set; }
    public string? DiscordWebhook { get; set; }
    public bool Debug { get; set; }
    public Dictionary<string, string[]>? BlockedDeviceNames { get; set; }
}
