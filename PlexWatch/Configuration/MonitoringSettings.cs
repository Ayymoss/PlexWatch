namespace PlexWatch.Configuration;

public class MonitoringSettings
{
    public const string SectionName = "Monitoring";

    public int PollingIntervalSeconds { get; set; } = 30;
    public Dictionary<string, string[]> BlockedDeviceNames { get; set; } = new();
    public bool SnapshotEnabled { get; set; }
    public string SnapshotDirectory { get; set; } = "_Snapshots";
}
