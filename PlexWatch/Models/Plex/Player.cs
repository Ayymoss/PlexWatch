namespace PlexWatch.Models.Plex;

public class Player
{
    public string? Address { get; set; }
    public string? Device { get; set; }
    public string? MachineIdentifier { get; set; }
    public string? Model { get; set; }
    public string? Platform { get; set; }
    public string? PlatformVersion { get; set; }
    public string? Product { get; set; }
    public string? Profile { get; set; }
    public string? RemotePublicAddress { get; set; }
    public string? PublicAddress { get; set; }
    public string? State { get; set; }
    public string? Title { get; set; }
    public string? Vendor { get; set; }
    public string? Version { get; set; }
    public string? Uuid { get; set; }
    public bool Local { get; set; }
    public bool Relayed { get; set; }
    public bool Secure { get; set; }
    public int? UserId { get; set; }
}
