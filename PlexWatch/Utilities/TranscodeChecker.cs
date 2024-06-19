using Humanizer;
using Microsoft.Extensions.Logging;
using PlexWatch.Enums;
using PlexWatch.Interfaces;
using PlexWatch.Models.Plex;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace PlexWatch.Utilities;

public class TranscodeChecker(ILogger<TranscodeChecker> logger, IPlexApi plexApi)
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task CheckForTranscodeAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var response = await plexApi.GetSessions();
            if (response.MediaContainer.Metadata is null || response.MediaContainer.Metadata.Count is 0) return;

            foreach (var responseMeta in response.MediaContainer.Metadata)
            {
                if (responseMeta.Type is MediaType.Clip) continue;
                await HandleMetadataAsync(responseMeta);
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

    private async Task HandleMetadataAsync(Metadata responseMeta)
    {
        var mediaType = responseMeta.Type;
        var ratingKey = responseMeta.RatingKey;
        var videoDecision = responseMeta.TranscodeSession?.VideoDecision ?? "Direct Play";
        var device = responseMeta.Player?.Device;
        var player = $"{responseMeta.Player?.Product}: {responseMeta.Player?.Title}";

        if (string.IsNullOrEmpty(ratingKey)) return;
        var contentMeta = mediaType is MediaType.Episode
            ? await plexApi.GetEpisodeMetadataAsync(ratingKey)
            : await plexApi.GetMovieMetadataAsync(ratingKey);

        var contentMedia = contentMeta.MediaContainer.Metadata?.First().Media?.FirstOrDefault();
        if (contentMedia is null) return;

        var responseMedia = responseMeta.Media?.FirstOrDefault();
        if (responseMedia is null) return;

        var sessionId = responseMeta.Session?.Id;
        if (sessionId is null) return;

        var mediaBitrate = contentMedia.Bitrate;
        var streamBitrate = responseMedia.Part?.First().Stream?.First().Bitrate;
        var streamVideoWidth = responseMedia.Width;
        var sourceVideoWidth = contentMedia.Width;
        if (!mediaBitrate.HasValue || !streamBitrate.HasValue || !streamVideoWidth.HasValue || !sourceVideoWidth.HasValue) return;

        var title = responseMeta.Type is MediaType.Episode
            ? $"{responseMeta.GrandparentTitle}: {responseMeta.Title}"
            : responseMeta.Title;

        var qualityProfile = GetQualityProfile(videoDecision, streamBitrate.Value, mediaBitrate.Value);
        var terminate = TerminateStream(sourceVideoWidth.Value, streamVideoWidth.Value, qualityProfile, device);
        var audioDecision = responseMeta.TranscodeSession?.AudioDecision ?? "Unknown";

        logger.LogInformation("Session -> {@LogData}", new
        {
            SessionId = sessionId,
            QualityProfile = qualityProfile,
            Terminate = terminate,
            UserTitle = responseMeta.User?.Title,
            RatingKey = ratingKey,
            Title = title,
            Type = mediaType,
            Device = device,
            Player = player,
            VideoDecision = videoDecision.Titleize(),
            SourceVideoWidth = sourceVideoWidth,
            StreamVideoWidth = streamVideoWidth,
            StreamBitrate = streamBitrate,
            MediaReportedBitrate = responseMedia.Bitrate?.ToString() ?? "No Bitrate",
            MediaExpectedBitrate = mediaBitrate,
            SessionBandwidth = responseMeta.Session?.Bandwidth?.ToString() ?? "No Bandwidth"
        });

        if (terminate is TerminationReason.Ok) return;
        logger.LogWarning("Terminating Session [{TerminationReason}] -> {@LogData}", terminate.Humanize().Titleize(), new
        {
            SessionId = sessionId,
            QualityProfile = qualityProfile,
            UserTitle = responseMeta.User?.Title,
            Title = title,
            VideoDecision = videoDecision.Titleize(),
            AudioDecision = audioDecision.Titleize()
        });

        var reason = GetReason(terminate);
        // Depending on the client, it will either keep the newline as expected, or replace it with a space.
        await plexApi.TerminateSessionAsync(sessionId, $"«ERROR»\n[Session ID: {sessionId}]," +
                                                       $"\n[Reason: {reason.Reason}]," +
                                                       $"\n[Message: {reason.Message}]");
    }

    private static (string Reason, string Message) GetReason(TerminationReason termination)
    {
        const string qualityMessage = "Adjust your Plex client's 'Remote Quality' to 'Original' or 'Maximum' via the settings.";
        return termination switch
        {
            TerminationReason.StreamWidthMismatch => ("Stream Width Mismatch", qualityMessage),
            TerminationReason.RemoteQualityUnset => ("Remote Quality Unset", qualityMessage),
            TerminationReason.IncorrectClient => ("Incorrect Client",
                "Use or download the Plex Desktop Client to stream this content."),
            _ => throw new ArgumentOutOfRangeException(nameof(termination), termination, "Invalid Termination Reason")
        };
    }

    private static TerminationReason TerminateStream(int sourceWidth, int streamWidth, string qualityProfile, string? device)
    {
        if (!string.IsNullOrWhiteSpace(device) && device.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            return TerminationReason.IncorrectClient;

        if (!qualityProfile.Equals("Original", StringComparison.OrdinalIgnoreCase))
            return TerminationReason.RemoteQualityUnset;

        if (sourceWidth != streamWidth)
            return TerminationReason.StreamWidthMismatch;

        return TerminationReason.Ok;
    }

    private string GetQualityProfile(string videoDecision, int streamBitrate, int sourceFileBitrate)
    {
        if (!videoDecision.Equals("transcode", StringComparison.OrdinalIgnoreCase)) return "Original";

        streamBitrate = streamBitrate is int.MaxValue ? 0 : streamBitrate;

        var key = _videoQualityProfiles.Keys
            .Where(b => streamBitrate <= b && b <= sourceFileBitrate)
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
