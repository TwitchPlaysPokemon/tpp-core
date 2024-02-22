using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Core.Configuration;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using static TPP.Core.Configuration.ConnectionConfig.Twitch;

namespace TPP.Core.Chat;

public sealed class TwitchChatModeChanger(
    ILogger<TwitchChatModeChanger> logger,
    TwitchApiProvider twitchApiProvider,
    ConnectionConfig.Twitch chatConfig
) : IChatModeChanger
{
    private async Task<ChatSettings> GetChatSettings()
    {
        TwitchAPI twitchApi = await twitchApiProvider.Get();
        GetChatSettingsResponse settingsResponse =
            await twitchApi.Helix.Chat.GetChatSettingsAsync(chatConfig.ChannelId, chatConfig.UserId);
        // From the Twitch API documentation https://dev.twitch.tv/docs/api/reference/#update-chat-settings
        //   'data': The list of chat settings. The list contains a single object with all the settings
        ChatSettingsResponseModel settings = settingsResponse.Data[0];
        return new ChatSettings
        {
            EmoteMode = settings.EmoteMode,
            FollowerMode = settings.FollowerMode,
            FollowerModeDuration = settings.FollowerModeDuration,
            SlowMode = settings.SlowMode,
            SlowModeWaitTime = settings.SlowModeWaitDuration,
            SubscriberMode = settings.SubscriberMode,
            UniqueChatMode = settings.UniqueChatMode,
            NonModeratorChatDelay = settings.NonModeratorChatDelay,
            NonModeratorChatDelayDuration = settings.NonModeratorChatDelayDuration,
        };
    }

    public async Task EnableEmoteOnly()
    {
        if (chatConfig.Suppressions.Contains(SuppressionType.Command) &&
            !chatConfig.SuppressionOverrides.Contains(chatConfig.Channel))
        {
            logger.LogDebug($"(suppressed) enabling emote only mode in #{chatConfig.Channel}");
            return;
        }

        logger.LogDebug($"enabling emote only mode in #{chatConfig.Channel}");
        TwitchAPI twitchApi = await twitchApiProvider.Get();
        ChatSettings chatSettings = await GetChatSettings();
        chatSettings.EmoteMode = true;
        await twitchApi.Helix.Chat.UpdateChatSettingsAsync(chatConfig.ChannelId, chatConfig.UserId, chatSettings);
    }

    public async Task DisableEmoteOnly()
    {
        if (chatConfig.Suppressions.Contains(SuppressionType.Command) &&
            !chatConfig.SuppressionOverrides.Contains(chatConfig.Channel))
        {
            logger.LogDebug($"(suppressed) disabling emote only mode in #{chatConfig.Channel}");
            return;
        }

        logger.LogDebug($"disabling emote only mode in #{chatConfig.Channel}");
        TwitchAPI twitchApi = await twitchApiProvider.Get();
        ChatSettings chatSettings = await GetChatSettings();
        chatSettings.EmoteMode = false;
        await twitchApi.Helix.Chat.UpdateChatSettingsAsync(chatConfig.ChannelId, chatConfig.UserId, chatSettings);
    }
}
