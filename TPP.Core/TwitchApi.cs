using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Chat;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace TPP.Core;

/// Wrapper around TwitchLib API that transparently refreshes tokens and retries requests at least once.
public class TwitchApi(
    ILoggerFactory loggerFactory,
    IClock clock,
    string? infiniteAccessToken,
    string? refreshToken,
    string appClientId,
    string appClientSecret)
{
    private readonly TwitchApiProvider _twitchApiProvider = new(
        loggerFactory, clock, infiniteAccessToken, refreshToken, appClientId, appClientSecret);

    private async Task<T> Retrying<T>(Func<TwitchAPI, Task<T>> action)
    {
        try
        {
            return await action(await _twitchApiProvider.Get());
        }
        catch (HttpResponseException)
        {
            // Try everything twice. E.g. 500 Internal server errors or unexpectedly expired tokens are retryable.
            // If it's an actual issue, it will be thrown a second time anyway.
            await _twitchApiProvider.Invalidate();
            return await action(await _twitchApiProvider.Get());
        }
    }

    private async Task Retrying(Func<TwitchAPI, Task> action)
    {
        await Retrying(async api =>
        {
            await action(api);
            return (byte)0;
        });
    }

    // Chat (and whispers)
    public Task<GetChattersResponse> GetChattersAsync(
        string broadcasterId, string moderatorId, int first = 100, string? after = null) =>
        Retrying(api => api.Helix.Chat.GetChattersAsync(broadcasterId, moderatorId, first, after));
    public Task<GetChatSettingsResponse> GetChatSettingsAsync(string broadcasterId, string moderatorId) =>
        Retrying(api => api.Helix.Chat.GetChatSettingsAsync(broadcasterId, moderatorId));
    public Task UpdateChatSettingsAsync(string broadcasterId, string moderatorId, ChatSettings settings) =>
        Retrying(api => api.Helix.Chat.UpdateChatSettingsAsync(broadcasterId, moderatorId, settings));
    public Task SendChatMessage(string broadcasterId, string senderUserId, string message) =>
        Retrying(api => api.Helix.Chat.SendChatMessage(broadcasterId, senderUserId, message));
    public Task SendWhisperAsync(string fromUserId, string toUserId, string message, bool newRecipient) =>
        Retrying(api => api.Helix.Whispers.SendWhisperAsync(fromUserId, toUserId, message, newRecipient));

    // Users
    public Task<GetUsersResponse> GetUsersAsync(List<string> logins) =>
        Retrying(api => api.Helix.Users.GetUsersAsync(logins: logins));

    // Streams
    public Task<GetStreamsResponse> GetStreamsAsync(List<string>? userIds = null, List<string>? userLogins = null,
        int first = 20) =>
        Retrying(api => api.Helix.Streams.GetStreamsAsync(first: first, userIds: userIds, userLogins: userLogins));

    // Moderation
    public Task DeleteChatMessagesAsync(string broadcasterId, string moderatorId, string messageId) =>
        Retrying(api => api.Helix.Moderation.DeleteChatMessagesAsync(broadcasterId, moderatorId, messageId));
    public Task BanUserAsync(string broadcasterId, string moderatorId, BanUserRequest banUserRequest) =>
        Retrying(api => api.Helix.Moderation.BanUserAsync(broadcasterId, moderatorId, banUserRequest));
    public Task UnbanUserAsync(string broadcasterId, string moderatorId, string userId) =>
        Retrying(api => api.Helix.Moderation.UnbanUserAsync(broadcasterId, moderatorId, userId));
}
