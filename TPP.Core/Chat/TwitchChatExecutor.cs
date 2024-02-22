using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Configuration;
using TPP.Core.Moderation;
using TPP.Model;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using static TPP.Core.Configuration.ConnectionConfig.Twitch;

namespace TPP.Core.Chat;

public sealed class TwitchChatExecutor(
    ILogger<TwitchChatExecutor> logger,
    TwitchApiProvider twitchApiProvider,
    ConnectionConfig.Twitch chatConfig)
    : IExecutor
{
    private bool AreCommandsSuppressed() => chatConfig.Suppressions.Contains(SuppressionType.Command);
    private bool IsChannelExempt() => chatConfig.SuppressionOverrides.Contains(chatConfig.Channel);
    private bool IsUserExempt(User user) => chatConfig.SuppressionOverrides.Contains(user.SimpleName);
    private bool IsChannelAndUserExempt(User user) => IsChannelExempt() && IsUserExempt(user);

    public async Task DeleteMessage(string messageId)
    {
        if (AreCommandsSuppressed() && !IsChannelExempt())
        {
            logger.LogDebug($"(suppressed) deleting message {messageId} in #{chatConfig.Channel}");
            return;
        }

        logger.LogDebug($"deleting message {messageId} in #{chatConfig.Channel}");
        TwitchAPI twitchApi = await twitchApiProvider.Get();
        await twitchApi.Helix.Moderation.DeleteChatMessagesAsync(chatConfig.ChannelId, chatConfig.UserId, messageId);
    }

    public async Task Timeout(User user, string? message, Duration duration)
    {
        if (AreCommandsSuppressed() && !IsChannelAndUserExempt(user))
        {
            logger.LogDebug($"(suppressed) time out {user} for {duration} in #{chatConfig.Channel}: {message}");
            return;
        }

        logger.LogDebug($"time out {user} for {duration} in #{chatConfig.Channel}: {message}");
        TwitchAPI twitchApi = await twitchApiProvider.Get();
        var banUserRequest = new BanUserRequest
        {
            UserId = user.Id,
            Duration = (int)duration.TotalSeconds,
            Reason = message ?? "no timeout reason was given",
        };
        await twitchApi.Helix.Moderation.BanUserAsync(chatConfig.ChannelId, chatConfig.UserId, banUserRequest);
    }

    public async Task Ban(User user, string? message)
    {
        if (AreCommandsSuppressed() && !IsChannelAndUserExempt(user))
        {
            logger.LogDebug($"(suppressed) ban {user} in #{chatConfig.Channel}: {message}");
            return;
        }

        logger.LogDebug($"ban {user} in #{chatConfig.Channel}: {message}");
        TwitchAPI twitchApi = await twitchApiProvider.Get();
        var banUserRequest = new BanUserRequest
        {
            UserId = user.Id,
            Duration = null,
            Reason = message ?? "no ban reason was given",
        };
        await twitchApi.Helix.Moderation.BanUserAsync(chatConfig.ChannelId, chatConfig.UserId, banUserRequest);
    }

    public async Task Unban(User user, string? message)
    {
        if (AreCommandsSuppressed() && !IsChannelAndUserExempt(user))
        {
            logger.LogDebug($"(suppressed) unban {user} in #{chatConfig.Channel}: {message}");
            return;
        }

        logger.LogDebug($"unban {user} in #{chatConfig.Channel}: {message}");
        TwitchAPI twitchApi = await twitchApiProvider.Get();
        await twitchApi.Helix.Moderation.UnbanUserAsync(chatConfig.ChannelId, chatConfig.UserId, user.Id);
    }
}
