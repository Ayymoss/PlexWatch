using Humanizer;
using Microsoft.Extensions.Logging;
using PlexWatch.Enums;
using PlexWatch.Interfaces;
using PlexWatch.Models;
using PlexWatch.Models.Plex;

namespace PlexWatch.Services;

public class SessionContextFactory(IPlexApi plexApi, ILogger<SessionContextFactory> logger)
{
    /// <summary>
    /// Transforms raw Plex session metadata into <see cref="SessionContext"/> objects.
    /// Skips sessions with incomplete metadata, logging a warning for each.
    /// </summary>
    public async Task<List<SessionContext>> BuildContextsAsync(List<Metadata> sessions)
    {
        var contexts = new List<SessionContext>();
        foreach (var session in sessions)
        {
            var context = await CreateAsync(session);
            if (context is not null) contexts.Add(context);
        }

        return contexts;
    }

    private async Task<SessionContext?> CreateAsync(Metadata session)
    {
        var userTitle = session.User?.Title ?? "unknown";
        var title = session.Title ?? "unknown";

        var ratingKey = session.RatingKey;
        if (string.IsNullOrEmpty(ratingKey))
        {
            logger.LogWarning("Dropping session for {User} ({Title}): missing RatingKey", userTitle, title);
            return null;
        }

        var sessionId = session.Session?.Id;
        if (sessionId is null)
        {
            logger.LogWarning("Dropping session for {User} ({Title}): missing Session.Id", userTitle, title);
            return null;
        }

        var category = DeterminePlaybackCategory(session.TranscodeSession);
        var streamMedia = ResolveStreamMedia(session);
        if (streamMedia is null)
        {
            logger.LogWarning("Dropping session for {User} ({Title}): no Media entry found", userTitle, title);
            return null;
        }

        var sourceMedia = await ResolveSourceMediaAsync(session, category);
        if (sourceMedia is null)
        {
            logger.LogWarning("Dropping session for {User} ({Title}): could not resolve source media (RatingKey={RatingKey})",
                userTitle, title, ratingKey);
            return null;
        }

        var streamWidth = streamMedia.Width;
        var streamBitrate = streamMedia.Bitrate;
        var sourceWidth = sourceMedia.Width;
        var sourceBitrate = sourceMedia.Bitrate;

        if (!streamWidth.HasValue || !streamBitrate.HasValue || !sourceWidth.HasValue || !sourceBitrate.HasValue)
        {
            logger.LogWarning("Dropping session for {User} ({Title}): missing numeric field(s) — " +
                "SourceBitrate={SourceBitrate}, StreamBitrate={StreamBitrate}, SourceWidth={SourceWidth}, StreamWidth={StreamWidth}",
                userTitle, title, sourceBitrate, streamBitrate, sourceWidth, streamWidth);
            return null;
        }

        // For non-video transcodes, source and stream widths are equal (video is untouched)
        var effectiveSourceWidth = category is PlaybackCategory.VideoTranscode
            ? sourceWidth.Value
            : streamWidth.Value;
        var effectiveStreamWidth = streamWidth.Value;

        var videoDecision = session.TranscodeSession?.VideoDecision ?? "Direct Play";
        var audioDecision = session.TranscodeSession?.AudioDecision ?? "Direct Play";

        return new SessionContext
        {
            SessionId = sessionId,
            UserTitle = session.User?.Title,
            Title = session.Type is MediaType.Episode
                ? $"{session.GrandparentTitle}: {session.Title}"
                : session.Title,
            MediaType = session.Type,
            PlaybackCategory = category,
            RatingKey = ratingKey,
            Device = session.Player?.Device,
            Player = $"{session.Player?.Product}: {session.Player?.Title}",
            QualityProfile = ResolveQualityProfile(videoDecision, streamBitrate.Value, sourceBitrate.Value),
            VideoDecision = videoDecision.Transform(To.TitleCase),
            AudioDecision = audioDecision.Transform(To.TitleCase),
            SourceVideoWidth = effectiveSourceWidth,
            StreamVideoWidth = effectiveStreamWidth,
            StreamBitrate = streamBitrate.Value,
            MediaBitrate = sourceBitrate.Value,
            SessionBandwidth = session.Session?.Bandwidth,
            SourceVideoCodec = session.TranscodeSession?.SourceVideoCodec ?? streamMedia.VideoCodec,
            StreamVideoCodec = session.TranscodeSession?.VideoCodec ?? streamMedia.VideoCodec
        };
    }

    private static PlaybackCategory DeterminePlaybackCategory(TranscodeSession? transcode)
    {
        if (transcode is null) return PlaybackCategory.DirectPlay;

        var video = transcode.VideoDecision;
        var audio = transcode.AudioDecision;

        if (video?.Equals("transcode", StringComparison.OrdinalIgnoreCase) == true)
            return PlaybackCategory.VideoTranscode;

        if (video?.Equals("copy", StringComparison.OrdinalIgnoreCase) == true
            && audio?.Equals("copy", StringComparison.OrdinalIgnoreCase) == true)
            return PlaybackCategory.ContainerCopy;

        if (video?.Equals("copy", StringComparison.OrdinalIgnoreCase) == true)
            return PlaybackCategory.AudioOnlyTranscode;

        return PlaybackCategory.DirectPlay;
    }

    /// <summary>
    /// Returns the Media entry representing the active stream output.
    /// For transcodes, this is the entry with <c>Selected = true</c>.
    /// Falls back to the first entry for direct play.
    /// </summary>
    private static Media? ResolveStreamMedia(Metadata session)
    {
        return session.Media?.FirstOrDefault(m => m.Selected == true)
               ?? session.Media?.FirstOrDefault();
    }

    /// <summary>
    /// Resolves the source (original file) media properties.
    /// For non-video-transcodes, the stream media IS the source (video untouched).
    /// For video transcodes: tries to find a non-selected Media entry with a file path first,
    /// then falls back to a secondary API call for single-version content.
    /// </summary>
    private async Task<Media?> ResolveSourceMediaAsync(Metadata session, PlaybackCategory category)
    {
        if (category is not PlaybackCategory.VideoTranscode)
            return ResolveStreamMedia(session);

        // Multi-version content: look for a non-selected Media entry that has a file path
        var sourceFromSession = FindSourceMediaInSession(session);
        if (sourceFromSession is not null) return sourceFromSession;

        // Single-version content: fall back to secondary API call
        logger.LogDebug("Fetching content metadata for {Title} (RatingKey={RatingKey})",
            session.Title, session.RatingKey);

        var contentMeta = session.Type is MediaType.Episode
            ? await plexApi.GetEpisodeMetadataAsync(session.RatingKey)
            : await plexApi.GetMovieMetadataAsync(session.RatingKey);

        return contentMeta.MediaContainer.Metadata?.FirstOrDefault()?.Media?.FirstOrDefault();
    }

    /// <summary>
    /// For multi-version content, finds the source Media entry from the session response.
    /// Matches by comparing the entry's video codec against TranscodeSession.SourceVideoCodec
    /// when multiple non-selected entries exist.
    /// </summary>
    private static Media? FindSourceMediaInSession(Metadata session)
    {
        if (session.Media is null || session.Media.Count < 2) return null;

        var sourceCodec = session.TranscodeSession?.SourceVideoCodec;
        var candidates = session.Media
            .Where(m => m.Selected != true && m.Part?.Any(p => p.File is not null) == true)
            .ToList();

        if (candidates.Count is 0) return null;
        if (candidates.Count is 1) return candidates[0];

        // Multiple source versions: prefer the one matching the transcode source codec
        if (sourceCodec is not null)
        {
            var codecMatch = candidates.FirstOrDefault(m =>
                m.VideoCodec?.Equals(sourceCodec, StringComparison.OrdinalIgnoreCase) == true);
            if (codecMatch is not null) return codecMatch;
        }

        return candidates[0];
    }

    /// <summary>
    /// Maps the stream's bitrate to a Plex quality profile name (e.g. "4 Mbps 720p").
    /// Returns "Original" if the stream is not transcoding or no matching profile is found.
    /// </summary>
    private static string ResolveQualityProfile(string videoDecision, int streamBitrate, int sourceFileBitrate)
    {
        if (!videoDecision.Equals("transcode", StringComparison.OrdinalIgnoreCase)) return "Original";

        streamBitrate = streamBitrate is int.MaxValue ? 0 : streamBitrate;

        var key = VideoQualityProfiles.Keys
            .Where(b => streamBitrate <= b && b <= sourceFileBitrate)
            .DefaultIfEmpty(int.MinValue)
            .Min();

        return VideoQualityProfiles.TryGetValue(key, out var profile) ? profile : "Original";
    }

    private static readonly SortedDictionary<int, string> VideoQualityProfiles = new()
    {
        { 20000, "20 Mbps 1080p" },
        { 12000, "12 Mbps 1080p" },
        { 10000, "10 Mbps 1080p" },
        { 8000, "8 Mbps 1080p" },
        { 4000, "4 Mbps 720p" },
        { 3000, "3 Mbps 720p" },
        { 2000, "2 Mbps 720p" },
        { 1500, "1.5 Mbps 480p" },
        { 720, "0.7 Mbps 328p" },
        { 320, "0.3 Mbps 240p" },
        { 208, "0.2 Mbps 160p" },
        { 96, "0.096 Mbps" },
        { 64, "0.064 Mbps" }
    };
}
