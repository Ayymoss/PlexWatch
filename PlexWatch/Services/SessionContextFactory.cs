using System.Text.Json;
using Humanizer;
using Microsoft.Extensions.Logging;
using PlexWatch.Enums;
using PlexWatch.Interfaces;
using PlexWatch.Models;
using PlexWatch.Models.Plex;
using PlexWatch.Utilities;

namespace PlexWatch.Services;

public class SessionContextFactory(
    IPlexApi plexApi,
    SessionSnapshotService snapshotService,
    ILogger<SessionContextFactory> logger)
{
    /// <summary>
    /// Fetches all active Plex sessions and transforms them into <see cref="SessionContext"/> objects.
    /// Filters out clips and tracks, and skips sessions with incomplete metadata.
    /// Captures raw API snapshots for test fixture collection before any filtering.
    /// </summary>
    public async Task<List<SessionContext>> GetActiveSessionsAsync()
    {
        using var rawResponse = await plexApi.GetSessionsRaw();
        var json = await rawResponse.Content.ReadAsStringAsync();
        var rawJson = JsonDocument.Parse(json);
        snapshotService.TrySnapshot(rawJson.RootElement);

        var response = JsonSerializer.Deserialize<PlexRoot>(json, JsonConverters.JsonOptions);
        var sessions = response?.MediaContainer.Metadata;
        if (sessions is null || sessions.Count is 0) return [];

        var contexts = new List<SessionContext>();
        foreach (var session in sessions)
        {
            if (session.Type is MediaType.Clip or MediaType.Track) continue;

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

        var contentMeta = session.Type is MediaType.Episode
            ? await plexApi.GetEpisodeMetadataAsync(ratingKey)
            : await plexApi.GetMovieMetadataAsync(ratingKey);

        var contentMedia = contentMeta.MediaContainer.Metadata?.FirstOrDefault()?.Media?.FirstOrDefault();
        if (contentMedia is null)
        {
            logger.LogWarning("Dropping session for {User} ({Title}): content metadata lookup returned no media (RatingKey={RatingKey})",
                userTitle, title, ratingKey);
            return null;
        }

        var responseMedia = session.Media?.FirstOrDefault(m => m.Selected == true) ?? session.Media?.FirstOrDefault();
        if (responseMedia is null)
        {
            logger.LogWarning("Dropping session for {User} ({Title}): session has no Media array", userTitle, title);
            return null;
        }

        var mediaBitrate = contentMedia.Bitrate;
        var streamBitrate = responseMedia.Bitrate;
        var streamVideoWidth = responseMedia.Width;
        var sourceVideoWidth = contentMedia.Width;
        if (!mediaBitrate.HasValue || !streamBitrate.HasValue || !streamVideoWidth.HasValue || !sourceVideoWidth.HasValue)
        {
            logger.LogWarning("Dropping session for {User} ({Title}): missing numeric field(s) — " +
                "MediaBitrate={MediaBitrate}, StreamBitrate={StreamBitrate}, StreamVideoWidth={StreamVideoWidth}, SourceVideoWidth={SourceVideoWidth}",
                userTitle, title, mediaBitrate, streamBitrate, streamVideoWidth, sourceVideoWidth);
            return null;
        }

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
            RatingKey = ratingKey,
            Device = session.Player?.Device,
            Player = $"{session.Player?.Product}: {session.Player?.Title}",
            QualityProfile = ResolveQualityProfile(videoDecision, streamBitrate.Value, mediaBitrate.Value),
            VideoDecision = videoDecision.Transform(To.TitleCase),
            AudioDecision = audioDecision.Transform(To.TitleCase),
            SourceVideoWidth = sourceVideoWidth.Value,
            StreamVideoWidth = streamVideoWidth.Value,
            StreamBitrate = streamBitrate.Value,
            MediaBitrate = mediaBitrate.Value,
            SessionBandwidth = session.Session?.Bandwidth,
            MediaReportedBitrate = responseMedia.Bitrate
        };
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
