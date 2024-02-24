using System.Text.Json.Serialization;

namespace PlexEndTranscodeSession.Models;

public class ResponseData
{
    [JsonIgnore] public int? StreamCount => Sessions?.Count;
    [JsonPropertyName("sessions")] public List<Session>? Sessions { get; set; }
}
