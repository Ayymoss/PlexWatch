using PlexWatch.Models;
using Refit;

namespace PlexWatch.Interfaces;

public interface IDiscord
{
    [Post("")]
    Task SendWebhookAsync([Body] DiscordWebhookPayload payload);
}
