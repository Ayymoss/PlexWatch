using PlexWatch.Enums;

namespace PlexWatch.Utilities;

public class Configuration
{
    public string? PlexUrl { get; set; }
    public string? PlexToken { get; set; }
    public string? DiscordWebhook { get; set; }
    public bool Debug { get; set; }
    public TranscodeKickBehaviour TranscodeKickBehaviour { get; set; }
    public Dictionary<string, string[]>? BlockedDeviceNames { get; set; }
}
