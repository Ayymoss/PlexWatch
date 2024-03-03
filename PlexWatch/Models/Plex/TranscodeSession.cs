namespace PlexWatch.Models.Plex;

public class TranscodeSession
{
    public string? Key { get; set; }
    public bool Throttled { get; set; }
    public bool Complete { get; set; }
    public double Progress { get; set; }
    public int Size { get; set; }
    public double Speed { get; set; }
    public bool Error { get; set; }
    public int Duration { get; set; }
    public int Remaining { get; set; }
    public string? Context { get; set; }
    public string? SourceVideoCodec { get; set; }
    public string? SourceAudioCodec { get; set; }
    public string? VideoDecision { get; set; }
    public string? AudioDecision { get; set; }
    public string? Protocol { get; set; }
    public string? Container { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public int AudioChannels { get; set; }
    public bool TranscodeHwRequested { get; set; }
    public double TimeStamp { get; set; }
    public double MaxOffsetAvailable { get; set; }
    public double MinOffsetAvailable { get; set; }
}
