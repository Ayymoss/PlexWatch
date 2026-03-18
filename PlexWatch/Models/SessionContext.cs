using PlexWatch.Enums;

namespace PlexWatch.Models;

public record SessionContext
{
    public required string SessionId { get; init; }
    public required string? UserTitle { get; init; }
    public required string Title { get; init; }
    public required MediaType MediaType { get; init; }
    public required string RatingKey { get; init; }
    public required string? Device { get; init; }
    public required string Player { get; init; }
    public required string QualityProfile { get; init; }
    public required string VideoDecision { get; init; }
    public required string AudioDecision { get; init; }
    public required int SourceVideoWidth { get; init; }
    public required int StreamVideoWidth { get; init; }
    public required int StreamBitrate { get; init; }
    public required int MediaBitrate { get; init; }
    public required int? SessionBandwidth { get; init; }
    public required int? MediaReportedBitrate { get; init; }
}
