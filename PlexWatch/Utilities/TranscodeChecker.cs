using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexWatch.Enums;
using PlexWatch.Interfaces;
using PlexWatch.Models.Plex;
using PlexWatch.Services;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace PlexWatch.Utilities;

public class TranscodeChecker
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<TranscodeChecker> _logger;
    private readonly IPlexApi _plexApi;
    private readonly DiscordWebhookService _discordWebhookService;
    private Configuration _configuration;

    public TranscodeChecker(ILogger<TranscodeChecker> logger, IPlexApi plexApi, IOptionsMonitor<Configuration> optionsMonitor,
        DiscordWebhookService discordWebhookService)
    {
        _logger = logger;
        _plexApi = plexApi;
        _discordWebhookService = discordWebhookService;
        _configuration = optionsMonitor.CurrentValue;
        optionsMonitor.OnChange(updated =>
        {
            _configuration = updated;
            logger.LogWarning("Configuration updated and reloaded...");
        });
    }

    public async Task CheckForTranscodeAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var response = await _plexApi.GetSessions();
            if (response.MediaContainer.Metadata is null || response.MediaContainer.Metadata.Count is 0) return;

            foreach (var responseMeta in response.MediaContainer.Metadata)
            {
                if (responseMeta.Type is MediaType.Clip) continue;
                await HandleMetadataAsync(responseMeta);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing scheduled action");
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
        var formattedPlayer = $"{responseMeta.Player?.Product}: {responseMeta.Player?.Title}";
        var userTitle = responseMeta.User?.Title;

        if (string.IsNullOrEmpty(ratingKey)) return;
        var contentMeta = mediaType is MediaType.Episode
            ? await _plexApi.GetEpisodeMetadataAsync(ratingKey)
            : await _plexApi.GetMovieMetadataAsync(ratingKey);

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

        var isBlockedClient = IsBlockedClient(userTitle, responseMeta.Player?.Title);
        var qualityProfile = GetQualityProfile(videoDecision, streamBitrate.Value, mediaBitrate.Value);
        var terminate = TerminateStream(sourceVideoWidth.Value, streamVideoWidth.Value, qualityProfile, device, isBlockedClient);
        var audioDecision = responseMeta.TranscodeSession?.AudioDecision ?? "Unknown";

        _logger.LogInformation("Session -> {@LogData}", new
        {
            SessionId = sessionId,
            QualityProfile = qualityProfile,
            Terminate = terminate,
            UserTitle = userTitle,
            RatingKey = ratingKey,
            Title = title,
            Type = mediaType,
            Device = device,
            Player = formattedPlayer,
            StreamBitrate = streamBitrate,
            SessionBandwidth = responseMeta.Session?.Bandwidth?.ToString() ?? "No Bandwidth",
            VideoDecision = videoDecision.Titleize(),
            SourceVideoWidth = sourceVideoWidth,
            StreamVideoWidth = streamVideoWidth,
            MediaExpectedBitrate = mediaBitrate,
            MediaReportedBitrate = responseMedia.Bitrate?.ToString() ?? "No Bitrate"
        });

        if (terminate is TerminationReason.Ok) return;
        _logger.LogWarning("Terminating Session [{TerminationReason}] -> {@LogData}", terminate.Humanize().Titleize(), new
        {
            SessionId = sessionId,
            QualityProfile = qualityProfile,
            UserTitle = userTitle,
            Title = title,
            VideoDecision = videoDecision.Titleize(),
            AudioDecision = audioDecision.Titleize()
        });

        var reason = GetReason(terminate);
        // Depending on the client, it will either keep the newline as expected, or replace it with a space.
        await _plexApi.TerminateSessionAsync(sessionId, $"«ERROR»\n[Session ID: {sessionId}]," +
                                                        $"\n[Reason: {reason.Reason}]," +
                                                        $"\n[Message: {reason.Message}]");
        await _discordWebhookService.SendAsync($"Terminating: {userTitle} - {terminate.Humanize().Titleize()}",
            $"**Session ID**: {sessionId}\n" +
            $"**Quality Profile**: {qualityProfile}\n" +
            $"**Rating Key**: {ratingKey}\n" +
            $"**Title**: {title}\n" +
            $"**Type**: {mediaType}\n" +
            $"**Device**: {device ?? "Unknown"}\n" +
            $"**Player**: {formattedPlayer}\n" +
            $"**Stream Bitrate**: {streamBitrate}\n" +
            $"**Session Bandwidth**: {responseMeta.Session?.Bandwidth?.ToString("N0") ?? "No Bandwidth"}\n" +
            $"**Video Decision**: {videoDecision.Titleize()}\n" +
            $"**Audio Decision**: {audioDecision.Titleize()}\n" +
            $"**Source Video Width**: {sourceVideoWidth}\n" +
            $"**Stream Video Width**: {streamVideoWidth}\n" +
            $"**Media Expected Bitrate**: {mediaBitrate:N0}\n" +
            $"**Media Reported Bitrate**: {responseMedia.Bitrate?.ToString("N0") ?? "No Bitrate"}\n");
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
            TerminationReason.BlockedClient => ("Prohibited Client", "This client has been blocked from usage."),
            _ => throw new ArgumentOutOfRangeException(nameof(termination), termination, "Invalid Termination Reason")
        };
    }

    private static TerminationReason TerminateStream(int sourceWidth, int streamWidth, string qualityProfile, string? device,
        bool isBlockedClient)
    {
        if (!string.IsNullOrWhiteSpace(device) && device.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            return TerminationReason.IncorrectClient;

        if (!qualityProfile.Equals("Original", StringComparison.OrdinalIgnoreCase))
            return TerminationReason.RemoteQualityUnset;

        if (Math.Abs(sourceWidth - streamWidth) > 0.1 * sourceWidth)
            return TerminationReason.StreamWidthMismatch;

        if (isBlockedClient)
            return TerminationReason.BlockedClient;

        return TerminationReason.Ok;
    }

    private bool IsBlockedClient(string? user, string? player)
    {
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(player) ||
            _configuration.BlockedDeviceNames.Count is 0) return false;

        if (!_configuration.BlockedDeviceNames.TryGetValue(user, out var players)) return false;
        return players.FirstOrDefault() == "*" || players.Contains(player);
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
