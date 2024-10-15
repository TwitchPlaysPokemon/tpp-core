using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Chat;
using TPP.Twitch.EventSub;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace TPP.Core;

/// Wrapper around TwitchLib API that transparently refreshes tokens and retries requests at least once.
public class TwitchApi(
    ILoggerFactory loggerFactory,
    IClock clock,
    string? botInfiniteAccessToken,
    string? botRefreshToken,
    string? channelInfiniteAccessToken,
    string? channelRefreshToken,
    string appClientId,
    string appClientSecret)
{
    private readonly TwitchApiProvider _botTwitchApiProvider = new(
        loggerFactory, clock, botInfiniteAccessToken, botRefreshToken, appClientId, appClientSecret);
    private readonly TwitchApiProvider _channelTwitchApiProvider = new(
        loggerFactory, clock, channelInfiniteAccessToken, channelRefreshToken, appClientId, appClientSecret);
    private readonly ILogger<TwitchApi> _logger = loggerFactory.CreateLogger<TwitchApi>();

    private async Task<T> Retrying<T>(TwitchApiProvider apiProvider, Func<TwitchAPI, Task<T>> action)
    {
        try
        {
            return await action(await apiProvider.Get());
        }
        catch (HttpResponseException ex)
        {
            _logger.LogDebug(ex, "TwitchAPI errored: {Error}", await ex.HttpResponse.Content.ReadAsStringAsync());
            switch ((int)ex.HttpResponse.StatusCode)
            {
                case 401:
                    // unexpectedly expired tokens are retryable if we successfully refresh the token.
                    await apiProvider.Invalidate();
                    return await action(await apiProvider.Get());
                case >= 500 and < 600:
                    // issues on Twitch's end may be transient and often don't occurr a second time
                    return await action(await apiProvider.Get());
                default:
                    // otherwise, assume it's an actual error with our request and don't retry it
                    throw;
            }
        }
    }

    private async Task Retrying(TwitchApiProvider apiProvider, Func<TwitchAPI, Task> action)
    {
        await Retrying(apiProvider, async api =>
        {
            await action(api);
            return (byte)0;
        });
    }

    private Task<T> RetryingBot<T>(Func<TwitchAPI, Task<T>> action) => Retrying(_botTwitchApiProvider, action);
    private Task<T> RetryingChannel<T>(Func<TwitchAPI, Task<T>> action) => Retrying(_channelTwitchApiProvider, action);
    private Task RetryingBot(Func<TwitchAPI, Task> action) => Retrying(_botTwitchApiProvider, action);
    private Task RetryingChannel(Func<TwitchAPI, Task> action) => Retrying(_channelTwitchApiProvider, action);

    // Meta
    public Task<ValidateAccessTokenResponse> GetBotTokenInfo() =>
        RetryingBot(api => api.Auth.ValidateAccessTokenAsync());
    public Task<ValidateAccessTokenResponse> GetChannelTokenInfo() =>
        RetryingChannel(api => api.Auth.ValidateAccessTokenAsync());

    // Chat (and whispers)
    public Task<GetChattersResponse> GetChattersAsync(
        string broadcasterId, string moderatorId, int first = 100, string? after = null) =>
        RetryingBot(api => api.Helix.Chat.GetChattersAsync(broadcasterId, moderatorId, first, after));
    public Task<GetChatSettingsResponse> GetChatSettingsAsync(string broadcasterId, string moderatorId) =>
        RetryingBot(api => api.Helix.Chat.GetChatSettingsAsync(broadcasterId, moderatorId));
    public Task UpdateChatSettingsAsync(string broadcasterId, string moderatorId, ChatSettings settings) =>
        RetryingBot(api => api.Helix.Chat.UpdateChatSettingsAsync(broadcasterId, moderatorId, settings));
    public Task SendChatMessage(string broadcasterId, string senderUserId, string message,
        string? replyParentMessageId = null) =>
        RetryingBot(api =>
            api.Helix.Chat.SendChatMessage(broadcasterId, senderUserId, message,
                replyParentMessageId: replyParentMessageId));
    public Task SendWhisperAsync(string fromUserId, string toUserId, string message, bool newRecipient) =>
        RetryingBot(api => api.Helix.Whispers.SendWhisperAsync(fromUserId, toUserId, message, newRecipient));

    // Users
    public Task<GetUsersResponse> GetUsersAsync(List<string> ids) =>
        RetryingBot(api => api.Helix.Users.GetUsersAsync(ids: ids));

    // Streams
    public Task<GetStreamsResponse> GetStreamsAsync(List<string>? userIds, int first = 20) =>
        RetryingBot(api => api.Helix.Streams.GetStreamsAsync(first: first, userIds: userIds));

    // Moderation
    public Task DeleteChatMessagesAsync(string broadcasterId, string moderatorId, string messageId) =>
        RetryingBot(api => api.Helix.Moderation.DeleteChatMessagesAsync(broadcasterId, moderatorId, messageId));
    public Task BanUserAsync(string broadcasterId, string moderatorId, BanUserRequest banUserRequest) =>
        RetryingBot(api => api.Helix.Moderation.BanUserAsync(broadcasterId, moderatorId, banUserRequest));
    public Task UnbanUserAsync(string broadcasterId, string moderatorId, string userId) =>
        RetryingBot(api => api.Helix.Moderation.UnbanUserAsync(broadcasterId, moderatorId, userId));

    // EventSub
    public Task<CreateEventSubSubscriptionResponse> SubscribeToEventSubBot<T>(
        string sessionId, Dictionary<string, string> condition)
        where T : INotification, IHasSubscriptionType =>
        RetryingBot(api => api.Helix.EventSub.CreateEventSubSubscriptionAsync(
            T.SubscriptionType, T.SubscriptionVersion, condition,
            EventSubTransportMethod.Websocket, websocketSessionId: sessionId));
    public Task<bool> DeleteEventSubSubscriptionAsyncBot(string subscriptionId) =>
        RetryingBot(api => api.Helix.EventSub.DeleteEventSubSubscriptionAsync(subscriptionId));
    public Task<CreateEventSubSubscriptionResponse> SubscribeToEventSubChannel<T>(
        string sessionId, Dictionary<string, string> condition)
        where T : INotification, IHasSubscriptionType =>
        RetryingChannel(api => api.Helix.EventSub.CreateEventSubSubscriptionAsync(
            T.SubscriptionType, T.SubscriptionVersion, condition,
            EventSubTransportMethod.Websocket, websocketSessionId: sessionId));
    public Task<bool> DeleteEventSubSubscriptionAsyncChannel(string subscriptionId) =>
        RetryingChannel(api => api.Helix.EventSub.DeleteEventSubSubscriptionAsync(subscriptionId));

    private enum ScopeType { Bot, Channel }

    private record ScopeInfo(string Scope, string NeededFor, ScopeType ScopeType)
    {
        public static ScopeInfo Bot(string scope, string neededFor) => new(scope, neededFor, ScopeType.Bot);
        public static ScopeInfo Channel(string scope, string neededFor) => new(scope, neededFor, ScopeType.Channel);
    }

    /// Mostly copied from TPP.Core's README.md
    private static readonly List<ScopeInfo> ScopeInfos =
    [
        ScopeInfo.Bot("chat:read",                      "Read messages from chat (via IRC/TMI)"),
        ScopeInfo.Bot("chat:edit",                      "Send messages to chat (via IRC/TMI)"),
        ScopeInfo.Bot("user:bot",                       "Appear in chat as bot"),
        ScopeInfo.Bot("user:read:chat",                 "Read messages from chat. (via EventSub)"),
        ScopeInfo.Bot("user:write:chat",                "Send messages to chat. (via Twitch API)"),
        ScopeInfo.Bot("user:manage:whispers",           "Sending and receiving whispers"),
        ScopeInfo.Bot("moderator:read:chatters",        "Read the chatters list in the channel (e.g. for badge drops)"),
        ScopeInfo.Bot("moderator:read:followers",       "Read the followers list (currently old core)"),
        ScopeInfo.Bot("moderator:manage:banned_users",  "Timeout, ban and unban users (tpp automod, mod commands)"),
        ScopeInfo.Bot("moderator:manage:chat_messages", "Delete chat messages (tpp automod, purge invalid bets)"),
        ScopeInfo.Bot("moderator:manage:chat_settings", "Change chat settings, e.g. emote-only mode (mod commands)"),
        ScopeInfo.Channel("channel:read:subscriptions", "Reacting to incoming subscriptions")
    ];
    private static readonly Dictionary<string, ScopeInfo> ScopeInfosPerScope = ScopeInfos
        .ToDictionary(scopeInfo => scopeInfo.Scope, scopeInfo => scopeInfo);

    public async Task<List<string>> DetectProblems(string botUsername, string channelName)
    {
        _logger.LogDebug("Validating API access token...");
        ValidateAccessTokenResponse botTokenInfo = await GetBotTokenInfo();
        _logger.LogInformation(
            "Successfully got Twitch API bot access token info! Client-ID: {ClientID}, User-ID: {UserID}, " +
            "Login: {Login}, Expires in: {Expires}s, Scopes: {Scopes}", botTokenInfo.ClientId,
            botTokenInfo.UserId, botTokenInfo.Login, botTokenInfo.ExpiresIn, botTokenInfo.Scopes);
        ValidateAccessTokenResponse channelTokenInfo = await GetChannelTokenInfo();
        _logger.LogInformation(
            "Successfully got Twitch API channel access token info! Client-ID: {ClientID}, User-ID: {UserID}, " +
            "Login: {Login}, Expires in: {Expires}s, Scopes: {Scopes}", channelTokenInfo.ClientId,
            channelTokenInfo.UserId, channelTokenInfo.Login, channelTokenInfo.ExpiresIn, channelTokenInfo.Scopes);

        List<string> oofs = [];

        // Validate correct usernames
        if (!botTokenInfo.Login.Equals(botUsername, StringComparison.InvariantCultureIgnoreCase))
            oofs.Add($"Bot token login '{botTokenInfo.Login}' does not match configured bot username '{botUsername}'");
        if (!channelTokenInfo.Login.Equals(channelName, StringComparison.InvariantCultureIgnoreCase))
            oofs.Add($"Channel token login '{botTokenInfo.Login}' does not match configured channel '{channelName}'");

        // Validate correct Client-IDs
        if (!botTokenInfo.ClientId.Equals(appClientId, StringComparison.InvariantCultureIgnoreCase))
            oofs.Add($"Bot token Client-ID '{botTokenInfo.ClientId}' does not match configured " +
                    $"App-Client-ID '{appClientId}'. Did you create the token using the wrong App-Client-ID?");
        if (!channelTokenInfo.ClientId.Equals(appClientId, StringComparison.InvariantCultureIgnoreCase))
            oofs.Add(
                $"Channel token Client-ID '{channelTokenInfo.ClientId}' does not match configured " +
                $"App-Client-ID '{appClientId}'. Did you create the token using the wrong App-Client-ID?");

        // Validate Scopes
        foreach ((string scope, ScopeInfo scopeInfo) in ScopeInfosPerScope)
        {
            if (scopeInfo.ScopeType == ScopeType.Bot && !botTokenInfo.Scopes.ToHashSet().Contains(scope))
                oofs.Add($"Missing Twitch-API scope '{scope}' (bot), needed for: {scopeInfo.NeededFor}");
            if (scopeInfo.ScopeType == ScopeType.Channel && !channelTokenInfo.Scopes.ToHashSet().Contains(scope))
                oofs.Add($"Missing Twitch-API scope '{scope}' (channel), needed for: {scopeInfo.NeededFor}");
        }

        return oofs;
    }
}
