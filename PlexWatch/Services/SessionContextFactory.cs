using Humanizer;
using PlexWatch.Enums;
using PlexWatch.Interfaces;
using PlexWatch.Models;
using PlexWatch.Models.Plex;

namespace PlexWatch.Services;

public class SessionContextFactory(IPlexApi plexApi)
{
    /// <summary>
    /// Fetches all active Plex sessions and transforms them into <see cref="SessionContext"/> objects.
    /// Filters out clips and tracks, and skips sessions with incomplete metadata.
    /// </summary>
    public async Task<List<SessionContext>> GetActiveSessionsAsync()
    {
        var response = await plexApi.GetSessions();
        var sessions = response.MediaContainer.Metadata;
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
        var ratingKey = session.RatingKey;
        if (string.IsNullOrEmpty(ratingKey)) return null;

        var sessionId = session.Session?.Id;
        if (sessionId is null) return null;

        var contentMeta = session.Type is MediaType.Episode
            ? await plexApi.GetEpisodeMetadataAsync(ratingKey)
            : await plexApi.GetMovieMetadataAsync(ratingKey);

        var contentMedia = contentMeta.MediaContainer.Metadata?.FirstOrDefault()?.Media?.FirstOrDefault();
        if (contentMedia is null) return null;

        var responseMedia = session.Media?.FirstOrDefault();
        if (responseMedia is null) return null;

        var mediaBitrate = contentMedia.Bitrate;
        var streamBitrate = responseMedia.Part?.FirstOrDefault()?.Stream?.FirstOrDefault()?.Bitrate;
        var streamVideoWidth = responseMedia.Width;
        var sourceVideoWidth = contentMedia.Width;
        if (!mediaBitrate.HasValue || !streamBitrate.HasValue || !streamVideoWidth.HasValue || !sourceVideoWidth.HasValue) return null;

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
