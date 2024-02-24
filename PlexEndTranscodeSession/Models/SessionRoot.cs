using System.Text.Json.Serialization;

namespace PlexEndTranscodeSession.Models;

public class SessionRoot
{
    [JsonPropertyName("response")] public ResponseRoot? ResponseRoot { get; set; }
}
