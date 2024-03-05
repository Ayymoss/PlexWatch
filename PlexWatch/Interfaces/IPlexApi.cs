using PlexWatch.Models.Plex;
using Refit;

namespace PlexWatch.Interfaces;

public interface IPlexApi
{
    [Get("/status/sessions")]
    Task<PlexRoot> GetSessions();

    [Get("/library/metadata/{metadataKey}/grandchildren")]
    Task<PlexRoot> GetEpisodeMetadataAsync([AliasAs("metadataKey")] string metadataKey);

    [Get("/library/metadata/{metadataKey}")]
    Task<PlexRoot> GetMovieMetadataAsync([AliasAs("metadataKey")] string metadataKey);

    [Get("/status/sessions/terminate")]
    Task<HttpResponseMessage> TerminateSessionAsync([Query("sessionId")] string sessionId, [Query("reason")] string? reason);
}
