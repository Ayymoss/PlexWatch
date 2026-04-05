namespace PlexWatch.Models.Plex;

public class TranscodeSession
{
    public string? VideoDecision { get; set; }
    public string? AudioDecision { get; set; }
    public string? SourceVideoCodec { get; set; }
    public string? SourceAudioCodec { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public string? Protocol { get; set; }
    public string? Container { get; set; }
    public int? AudioChannels { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool? TranscodeHwRequested { get; set; }
}
