using System.Text.Json.Serialization;
using PlexEndTranscodeSession.Utilities;

namespace PlexEndTranscodeSession.Models;

public class Session
{
    [JsonPropertyName("user")] public string User { get; set; }
    [JsonPropertyName("quality_profile")] public string QualityProfile { get; set; }
    [JsonPropertyName("audio_decision")] public string AudioDecision { get; set; }
    [JsonPropertyName("video_decision")] public string VideoDecision { get; set; }
    [JsonPropertyName("full_title")] public string FullTitle { get; set; }
    [JsonPropertyName("session_id")] public string SessionId { get; set; }

    [JsonPropertyName("session_key"), JsonConverter(typeof(StringToIntConverter))]
    public int SessionKey { get; set; }
}
