using Humanizer;
using Microsoft.Extensions.Logging;
using PlexWatch.Enums;
using PlexWatch.Interfaces;
using PlexWatch.Models;

namespace PlexWatch.Services;

public class DiscordNotifier(IDiscord discord, ILogger<DiscordNotifier> logger)
{
    private const int ColorRed = 0xED4245;
    private const int ColorGreen = 0x57F287;

    /// <summary>
    /// Sends a Discord embed notification when a session is terminated,
    /// including the reason, player details, and stream metadata.
    /// </summary>
    public async Task NotifyTerminationAsync(SessionContext session, TerminationReason reason)
    {
        var payload = new DiscordWebhookPayload
        {
            Username = "PlexWatch",
            Embeds =
            [
                new Embed
                {
                    Title = $"Stream Terminated: {session.UserTitle}",
                    Description = session.Title,
                    Color = ColorRed,
                    Fields =
                    [
                        new EmbedField { Name = "Reason", Value = reason.Humanize(LetterCasing.Title), Inline = true },
                        new EmbedField { Name = "Player", Value = session.Player, Inline = true },
                        new EmbedField { Name = "Device", Value = session.Device ?? "Unknown", Inline = true },
                        new EmbedField { Name = "Quality Profile", Value = session.QualityProfile, Inline = true },
                        new EmbedField { Name = "Video Decision", Value = session.VideoDecision, Inline = true },
                        new EmbedField { Name = "Audio Decision", Value = session.AudioDecision, Inline = true },
                        new EmbedField { Name = "Source Width", Value = session.SourceVideoWidth.ToString(), Inline = true },
                        new EmbedField { Name = "Stream Width", Value = session.StreamVideoWidth.ToString(), Inline = true },
                        new EmbedField { Name = "Stream Bitrate", Value = $"{session.StreamBitrate:N0} kbps", Inline = true },
                    ],
                    Footer = new EmbedFooter { Text = $"Session {session.SessionId}" },
                    Timestamp = DateTimeOffset.UtcNow.ToString("o")
                }
            ]
        };

        try
        {
            await discord.SendWebhookAsync(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Discord notification for session {SessionId}", session.SessionId);
        }
    }

    /// <summary>
    /// Sends a Discord embed with active stream details. Used for informational
    /// notifications about sessions that passed all rule checks.
    /// </summary>
    public async Task NotifySessionInfoAsync(SessionContext session)
    {
        var payload = new DiscordWebhookPayload
        {
            Username = "PlexWatch",
            Embeds =
            [
                new Embed
                {
                    Title = $"Active Stream: {session.UserTitle}",
                    Description = session.Title,
                    Color = ColorGreen,
                    Fields =
                    [
                        new EmbedField { Name = "Player", Value = session.Player, Inline = true },
                        new EmbedField { Name = "Quality Profile", Value = session.QualityProfile, Inline = true },
                        new EmbedField { Name = "Video Decision", Value = session.VideoDecision, Inline = true },
                    ],
                    Footer = new EmbedFooter { Text = $"Session {session.SessionId}" },
                    Timestamp = DateTimeOffset.UtcNow.ToString("o")
                }
            ]
        };

        try
        {
            await discord.SendWebhookAsync(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Discord session info for {SessionId}", session.SessionId);
        }
    }
}
