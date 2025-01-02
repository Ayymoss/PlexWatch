using PlexWatch.Interfaces;
using PlexWatch.Models;

namespace PlexWatch.Services;

public class DiscordWebhookService(IDiscord discord)
{
    public async Task SendAsync(string title, string description)
    {
        var payload = new DiscordWebhookPayload
        {
            Embeds =
            [
                new Embed
                {
                    Title = title,
                    Description = description,
                    Color = 0xff3b55
                }
            ]
        };
        await discord.SendWebhookAsync(payload);
    }
}
