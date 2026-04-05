using System.Text.Json;
using PlexWatch.Enums;
using PlexWatch.Interfaces;
using PlexWatch.Models.Plex;
using PlexWatch.Utilities;

namespace PlexWatch.Services;

public class PlexSessionProvider(
    IPlexApi plexApi,
    SessionSnapshotService snapshotService)
{
    /// <summary>
    /// Fetches active Plex sessions from the API, captures snapshots for novel playback configurations,
    /// and returns deserialized session metadata filtered to movies and episodes.
    /// </summary>
    public async Task<List<Metadata>> GetActiveSessionsAsync()
    {
        using var rawResponse = await plexApi.GetSessionsRaw();
        var json = await rawResponse.Content.ReadAsStringAsync();
        var rawJson = JsonDocument.Parse(json);
        snapshotService.TrySnapshot(rawJson.RootElement);

        var response = JsonSerializer.Deserialize<PlexRoot>(json, JsonConverters.JsonOptions);
        var sessions = response?.MediaContainer.Metadata;
        if (sessions is null || sessions.Count is 0) return [];

        return sessions
            .Where(s => s.Type is not (MediaType.Clip or MediaType.Track))
            .ToList();
    }
}
