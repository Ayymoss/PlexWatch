using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexWatch.Configuration;

namespace PlexWatch.Services;

public class SessionSnapshotService(
    IOptionsMonitor<MonitoringSettings> optionsMonitor,
    ILogger<SessionSnapshotService> logger)
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };
    private readonly HashSet<string> _seenFingerprints = [];

    /// <summary>
    /// Inspects the raw session JSON response and saves any sessions with a previously unseen
    /// playback fingerprint. The fingerprint is derived from the playback configuration, not the
    /// content — so two episodes transcoded identically are considered duplicates.
    /// </summary>
    public void TrySnapshot(JsonElement rawResponse)
    {
        var settings = optionsMonitor.CurrentValue;
        if (!settings.SnapshotEnabled) return;

        JsonElement metadataArray;
        if (!rawResponse.TryGetProperty("MediaContainer", out var container)
            || !container.TryGetProperty("Metadata", out metadataArray)
            || metadataArray.ValueKind != JsonValueKind.Array)
            return;

        foreach (var session in metadataArray.EnumerateArray())
        {
            var fingerprint = BuildFingerprint(session);
            if (fingerprint is null || !_seenFingerprints.Add(fingerprint))
                continue;

            SaveSnapshot(settings.SnapshotDirectory, session, fingerprint);
        }
    }

    private static string? BuildFingerprint(JsonElement session)
    {
        var videoDecision = GetTranscodeField(session, "videoDecision") ?? "directplay";
        var sourceVideoCodec = GetTranscodeField(session, "sourceVideoCodec");
        var streamVideoCodec = GetTranscodeField(session, "videoCodec");
        var clientPlatform = GetPlayerField(session, "platform");

        // Get resolution from the selected (active) media entry, falling back to first
        var (sourceWidth, streamWidth) = GetResolutions(session);

        if (clientPlatform is null) return null;

        var raw = $"{videoDecision}|{sourceVideoCodec}|{streamVideoCodec}|{sourceWidth}|{streamWidth}|{clientPlatform}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..12];
        return hash;
    }

    private static (string? sourceWidth, string? streamWidth) GetResolutions(JsonElement session)
    {
        if (!session.TryGetProperty("Media", out var mediaArray)
            || mediaArray.ValueKind != JsonValueKind.Array)
            return (null, null);

        string? sourceWidth = null;
        string? streamWidth = null;

        foreach (var media in mediaArray.EnumerateArray())
        {
            var isSelected = media.TryGetProperty("selected", out var sel)
                             && sel.ValueKind == JsonValueKind.True;

            var width = media.TryGetProperty("width", out var w) ? w.ToString() : null;

            if (isSelected)
                streamWidth = width;
            else
                sourceWidth ??= width;
        }

        // Direct play: single media entry is both source and stream
        sourceWidth ??= streamWidth;
        streamWidth ??= sourceWidth;

        return (sourceWidth, streamWidth);
    }

    private static string? GetTranscodeField(JsonElement session, string field)
    {
        return session.TryGetProperty("TranscodeSession", out var ts)
               && ts.TryGetProperty(field, out var val)
            ? val.GetString()
            : null;
    }

    private static string? GetPlayerField(JsonElement session, string field)
    {
        return session.TryGetProperty("Player", out var player)
               && player.TryGetProperty(field, out var val)
            ? val.GetString()
            : null;
    }

    private void SaveSnapshot(string directory, JsonElement session, string fingerprint)
    {
        try
        {
            var dir = Path.IsPathRooted(directory)
                ? directory
                : Path.Join(AppContext.BaseDirectory, directory);
            Directory.CreateDirectory(dir);

            var user = session.TryGetProperty("User", out var u)
                       && u.TryGetProperty("title", out var t)
                ? t.GetString() ?? "unknown"
                : "unknown";

            var product = GetPlayerField(session, "product") ?? "unknown";
            var device = GetPlayerField(session, "device");
            var videoDecision = GetTranscodeField(session, "videoDecision") ?? "directplay";

            var playerLabel = device is not null ? $"{product}_{device}" : product;
            var fileName = $"{user}_{videoDecision}_{playerLabel}_{fingerprint}.json";
            var filePath = Path.Join(dir, fileName);

            if (File.Exists(filePath)) return;

            // Wrap in a MediaContainer to match the raw API shape for test fixtures
            var wrapper = new JsonObject
            {
                ["MediaContainer"] = new JsonObject
                {
                    ["size"] = 1,
                    ["Metadata"] = new JsonArray(JsonNode.Parse(session.GetRawText())!)
                }
            };

            File.WriteAllText(filePath, wrapper.ToJsonString(WriteOptions));
            logger.LogInformation("Saved session snapshot: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save session snapshot");
        }
    }
}
