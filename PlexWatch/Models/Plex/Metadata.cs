using System.Text.Json.Serialization;
using PlexWatch.Enums;
using PlexWatch.Utilities;

namespace PlexWatch.Models.Plex;

public class Metadata
{
    public long? AddedAt { get; set; }
    public long? Duration { get; set; }
    public string? GenuineMediaAnalysis { get; set; }
    public int? Index { get; set; }
    public string? Key { get; set; }
    public int? LastViewedAt { get; set; }
    [JsonConverter(typeof(StringToIntConverter))] public int? LibrarySectionId { get; set; }
    public string? RatingKey { get; set; }
    public string? SessionKey { get; set; }
    public string? Thumb { get; set; }
    public string? Title { get; set; }

    [JsonConverter(typeof(MediaTypeToEnumConverter))]
    public MediaType Type { get; set; }

    public int? UpdatedAt { get; set; }
    public int? ViewCount { get; set; }
    public int? ViewOffset { get; set; }
    public List<Media>? Media { get; set; }
    public User? User { get; set; }
    public Player? Player { get; set; }
    public Session? Session { get; set; }
    public TranscodeSession? TranscodeSession { get; set; }
    public string? Art { get; set; }
    public double? AudienceRating { get; set; }
    public string? AudienceRatingImage { get; set; }
    public string? ContentRating { get; set; }
    public string? GrandparentArt { get; set; }
    public string? GrandparentGuid { get; set; }
    public string? GrandparentKey { get; set; }
    public string? GrandparentRatingKey { get; set; }
    public string? GrandparentTheme { get; set; }
    public string? GrandparentThumb { get; set; }
    public string? GrandparentTitle { get; set; }
    public string? LibrarySectionKey { get; set; }
    public string? LibrarySectionTitle { get; set; }
    public string? LibrarySectionType { get; set; }
    public DateOnly OriginallyAvailableAt { get; set; }
    public string? ParentGuid { get; set; }
    public int? ParentIndex { get; set; }
    public string? ParentKey { get; set; }
    public string? ParentRatingKey { get; set; }
    public string? ParentThumb { get; set; }
    public string? ParentTitle { get; set; }
    public string? Summary { get; set; }
    public int? Year { get; set; }
}
