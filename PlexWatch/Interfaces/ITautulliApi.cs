using PlexWatch.Models;
using Refit;

namespace PlexWatch.Interfaces;

public interface ITautulliApi
{
    [Get("/api/v2?cmd=get_activity&apikey={apikey}")]
    Task<SessionRoot> GetActivityAsync(string apikey);

    [Get("/api/v2?cmd=terminate_session&session_key={sessionKey}&session_id={sessionId}&message={reason}&apikey={apikey}")]
    Task TerminateSessionAsync(string apikey, int sessionKey, string sessionId, string reason);
}
