using System.Text.Json.Serialization;
using PlexWatch.Enums;
using PlexWatch.Utilities;

namespace PlexWatch.Models.Plex;

public class Metadata
{
    public required string RatingKey { get; set; }
    public required string Title { get; set; }

    [JsonConverter(typeof(MediaTypeToEnumConverter))]
    public MediaType Type { get; set; }

    public List<Media>? Media { get; set; }
    public User? User { get; set; }
    public Player? Player { get; set; }
    public Session? Session { get; set; }
    public TranscodeSession? TranscodeSession { get; set; }
    public string? GrandparentTitle { get; set; }
}
