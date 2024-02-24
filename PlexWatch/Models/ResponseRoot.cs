using System.Text.Json.Serialization;

namespace PlexWatch.Models;

public class ResponseRoot
{
    [JsonPropertyName("result")] public string Result { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("data")] public ResponseData? Data { get; set; }
}
