using System;
using System.Collections.Generic;
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

    private async Task<T> Retrying<T>(TwitchApiProvider apiProvider, Func<TwitchAPI, Task<T>> action)
    {
        try
        {
            return await action(await apiProvider.Get());
        }
        catch (HttpResponseException ex)
        {
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
    public Task<ValidateAccessTokenResponse> ValidateBot() =>
        RetryingBot(api => api.Auth.ValidateAccessTokenAsync());
    public Task<ValidateAccessTokenResponse> ValidateChannel() =>
        RetryingChannel(api => api.Auth.ValidateAccessTokenAsync());

    // Chat (and whispers)
    public Task<GetChattersResponse> GetChattersAsync(
        string broadcasterId, string moderatorId, int first = 100, string? after = null) =>
        RetryingBot(api => api.Helix.Chat.GetChattersAsync(broadcasterId, moderatorId, first, after));
    public Task<GetChatSettingsResponse> GetChatSettingsAsync(string broadcasterId, string moderatorId) =>
        RetryingBot(api => api.Helix.Chat.GetChatSettingsAsync(broadcasterId, moderatorId));
    public Task UpdateChatSettingsAsync(string broadcasterId, string moderatorId, ChatSettings settings) =>
        RetryingBot(api => api.Helix.Chat.UpdateChatSettingsAsync(broadcasterId, moderatorId, settings));
    public Task SendChatMessage(string broadcasterId, string senderUserId, string message, string? replyParentMessageId = null) =>
        RetryingBot(api => api.Helix.Chat.SendChatMessage(broadcasterId, senderUserId, message, replyParentMessageId: replyParentMessageId));
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
}
