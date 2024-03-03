using Humanizer;
using Microsoft.Extensions.Logging;
using PlexWatch.Enums;
using PlexWatch.Interfaces;

namespace PlexWatch.Utilities;

public class TranscodeChecker(ILogger<TranscodeChecker> logger, IPlexApi plexApi)
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task CheckForTranscode(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var response = await plexApi.GetSessions();
            if (response.MediaContainer.Metadata is null || response.MediaContainer.Metadata.Count is 0) return;

            foreach (var responseMeta in response.MediaContainer.Metadata)
            {
                var type = responseMeta.Type;
                var ratingKey = responseMeta.RatingKey;
                var videoDecision = responseMeta.TranscodeSession?.VideoDecision?.Titleize() ?? "Direct Play";

                if (string.IsNullOrEmpty(ratingKey)) continue;
                var contentMeta = type is MediaType.Movie
                    ? await plexApi.GetMovieMetadataAsync(ratingKey)
                    : await plexApi.GetShowMetadataAsync(ratingKey);

                var contentMedia = contentMeta.MediaContainer.Metadata?.First().Media?.FirstOrDefault();
                if (contentMedia is null) continue;

                var responseMedia = responseMeta.Media?.FirstOrDefault();
                if (responseMedia is null) continue;

                var sessionId = responseMeta.Session?.Id;
                if (sessionId is null) continue;

                var mediaBitrate = contentMedia.Bitrate;
                var streamBitrate = responseMedia.Part?.First().Stream?.First().Bitrate;

                if (!mediaBitrate.HasValue || !streamBitrate.HasValue) continue;

                var title = responseMeta.Type is MediaType.Episode
                    ? $"{responseMeta.GrandparentTitle}: {responseMeta.Title}"
                    : responseMeta.Title;

                var qualityProfile = GetQualityProfile(streamBitrate.Value, mediaBitrate.Value);
                var killStream = !qualityProfile.Equals("Original");

                logger.LogInformation(
                    "[{SessionId}] [{QualityProfile} -> {KillStream}] {UserTitle} [{RatingKey}] -> [VIDEO: {TranscodeDecision}] " +
                    "{Title} [MEDIA: {ResponseMediaBitrate}, STREAM: {StreamBitrate}] | EXPECTED: [MEDIA: {MediaBitrate}] | " +
                    "SESSION: [Bandwidth: {SessionBandwidth}]",
                    sessionId, qualityProfile, killStream, responseMeta.User?.Title, ratingKey, videoDecision, title,
                    responseMedia.Bitrate?.ToString() ?? "No Bitrate", streamBitrate, mediaBitrate,
                    responseMeta.Session?.Bandwidth?.ToString() ?? "No Bandwidth");

                if (!killStream) continue;
                logger.LogWarning(
                    "Terminating ({SessionId} - {User}) {Title} [Quality: {Quality}, Video Decision: {VideoDecision}, Audio Decision: {AudioDecision}]",
                    sessionId, responseMeta.User?.Title, title, qualityProfile, responseMeta.TranscodeSession?.VideoDecision?.Titleize(),
                    responseMeta.TranscodeSession?.AudioDecision?.Titleize());
                await plexApi.TerminateSessionAsync(sessionId,
                    $"«TRANSCODE»\n[Session ID: {sessionId}], [Detected Profile: {qualityProfile}],\n[Message: Adjust your Plex client's 'Remote Quality' to 'Original' or 'Maximum' via the settings.]");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error executing scheduled action");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private string GetQualityProfile(int stream, int source)
    {
        stream = stream == int.MaxValue ? 0 : stream;

        var key = _videoQualityProfiles.Keys
            .Where(b => stream <= b && b <= source)
            .DefaultIfEmpty(int.MinValue)
            .Min();

        return _videoQualityProfiles.TryGetValue(key, out var profile) ? profile : "Original";
    }

    private readonly SortedDictionary<int, string> _videoQualityProfiles = new()
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
