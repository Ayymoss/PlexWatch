using System.Text.Json.Serialization;

namespace PlexWatch.Models;

public class SessionRoot
{
    [JsonPropertyName("response")] public ResponseRoot? ResponseRoot { get; set; }
}
