using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Common;
using TPP.Core.Utils;
using TPP.Persistence;
using TPP.Twitch.EventSub;
using TPP.Twitch.EventSub.Notifications;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using static TPP.Core.EventUtils;
using User = TPP.Model.User;

namespace TPP.Core.Chat;

public class TwitchEventSubChat : IWithLifecycle, IMessageSource
{
    public event EventHandler<MessageEventArgs>? IncomingMessage;

    private readonly ILogger<TwitchEventSubChat> _logger;
    private readonly TwitchApiProvider _twitchApiProvider;
    private readonly IClock _clock;
    private readonly IUserRepo _userRepo;

    private readonly bool _coStreamInputsEnabled;
    private readonly bool _coStreamInputsOnlyLive;
    private readonly ICoStreamChannelsRepo _coStreamChannelsRepo;
    private readonly Dictionary<string, string> _coStreamEventSubSubscriptions = new();

    private readonly EventSubClient _client;
    private readonly string _channelId;
    private readonly string _userId;

    private ChannelChatSettingsUpdate.Event? _channelState;
    private readonly Dictionary<User, Instant> _lastInputPerUser = new();
    private Session? _session;

    public TwitchEventSubChat(
        ILoggerFactory loggerFactory,
        IClock clock,
        TwitchApiProvider twitchApiProvider,
        IUserRepo userRepo,
        string channelId,
        string userId,
        bool coStreamInputsEnabled,
        bool coStreamInputsOnlyLive,
        ICoStreamChannelsRepo coStreamChannelsRepo)
    {
        _logger = loggerFactory.CreateLogger<TwitchEventSubChat>();
        _twitchApiProvider = twitchApiProvider;
        _clock = clock;
        _userRepo = userRepo;
        _channelId = channelId;
        _userId = userId;
        _coStreamInputsEnabled = coStreamInputsEnabled;
        _coStreamInputsOnlyLive = coStreamInputsOnlyLive;
        _coStreamChannelsRepo = coStreamChannelsRepo;

        _client = new EventSubClient(loggerFactory, clock);
        _client.RevocationReceived += (_, revocation) =>
            _logger.LogError("received revocation for {SubscriptionType}: {Data}",
                revocation.Metadata.SubscriptionType,
                JsonSerializer.Serialize(revocation.Payload.Subscription.Condition));
        _client.NotificationReceived += (_, notification) =>
            TaskToVoidSafely(_logger, async () =>
            {
                if (notification is ChannelChatMessage chatMessage)
                    await MessageReceived(chatMessage);
                else if (notification is ChannelChatSettingsUpdate settingsUpdate)
                    _channelState = settingsUpdate.Payload.Event;
                else
                    _logger.LogWarning("received unhandled EventSub notification of type {Type}, payload: {Payload}",
                        notification.Metadata.SubscriptionType, notification.Payload);
            });
        _client.Connected += (_, welcome) =>
            TaskToVoidSafely(_logger, () => SetUpSubscriptions(welcome.Payload.Session));
    }

    private async Task SetUpSubscriptions(Session session)
    {
        _session = session;
        _logger.LogDebug("Setting up EventSub subscriptions");
        await session.SubscribeWithTwitchLibApi<ChannelChatMessage>(
            (await _twitchApiProvider.Get()).Helix.EventSub,
            new ChannelChatMessage.Condition(BroadcasterUserId: _channelId, UserId: _userId).AsDict());
        await session.SubscribeWithTwitchLibApi<ChannelChatSettingsUpdate>(
            (await _twitchApiProvider.Get()).Helix.EventSub,
            new ChannelChatSettingsUpdate.Condition(BroadcasterUserId: _channelId, UserId: _userId).AsDict());

        if (_coStreamInputsEnabled)
        {
            foreach (string channelId in await _coStreamChannelsRepo.GetJoinedChannels())
            {
                try
                {
                    var response = await session.SubscribeWithTwitchLibApi<ChannelChatMessage>(
                        (await _twitchApiProvider.Get()).Helix.EventSub,
                        new ChannelChatMessage.Condition(BroadcasterUserId: channelId, UserId: _userId).AsDict());
                    _coStreamEventSubSubscriptions[channelId] = response.Subscriptions[0].Id;
                }
                catch (HttpResponseException ex)
                {
                    _logger.LogError(ex,
                        "Failed to join co-stream channel ID {ChannelId}, removing channel. Maybe the channel was " +
                        "deleted, or the database contained old data (channel names instead of IDs)", channelId);
                    await _coStreamChannelsRepo.Remove(channelId);
                }
            }
        }
    }

    public enum JoinResult { Ok, NotEnabled, AlreadyJoined, UserNotFound, StreamOffline }
    public async Task<JoinResult> Join(string userId)
    {
        if (!_coStreamInputsEnabled)
            return JoinResult.NotEnabled;

        TwitchAPI api = await _twitchApiProvider.Get();
        GetUsersResponse getUsersResponse = await api.Helix.Users.GetUsersAsync(ids: [userId]);
        var user = getUsersResponse.Users.FirstOrDefault();
        if (user == null)
            return JoinResult.UserNotFound;

        if (await _coStreamChannelsRepo.IsJoined(userId))
            return JoinResult.AlreadyJoined;

        if (_coStreamInputsOnlyLive)
        {
            GetStreamsResponse getStreamsResponse =
                await api.Helix.Streams.GetStreamsAsync(userIds: [userId]);
            Stream? stream = getStreamsResponse.Streams.FirstOrDefault();
            if (stream is null)
                return JoinResult.StreamOffline;
        }

        await _coStreamChannelsRepo.Add(userId, user.ProfileImageUrl);
        var response = await _session!.SubscribeWithTwitchLibApi<ChannelChatMessage>(
            (await _twitchApiProvider.Get()).Helix.EventSub,
            new ChannelChatMessage.Condition(BroadcasterUserId: userId, UserId: _userId).AsDict());
        _coStreamEventSubSubscriptions[userId] = response.Subscriptions[0].Id;
        await api.Helix.Chat.SendChatMessage(userId, _userId, "Joined channel, hello!");

        return JoinResult.Ok;
    }

    public enum LeaveResult { Ok, NotJoined }
    public async Task<LeaveResult> Leave(string userId)
    {
        if (!await _coStreamChannelsRepo.IsJoined(userId))
            return LeaveResult.NotJoined;
        TwitchAPI api = await _twitchApiProvider.Get();
        try
        {
            await api.Helix.Chat.SendChatMessage(userId, _userId, "Leaving channel, goodbye!");
        }
        catch (HttpResponseException ex)
        {
            _logger.LogError(ex, "Failed sending goodbye message to {ChannelID} after leaving, ignoring", userId);
        }
        if (_coStreamEventSubSubscriptions.TryGetValue(userId, out string? subscriptionId))
            await api.Helix.EventSub.DeleteEventSubSubscriptionAsync(subscriptionId);
        _coStreamEventSubSubscriptions.Remove(userId);
        await _coStreamChannelsRepo.Remove(userId);
        return LeaveResult.Ok;
    }

    /// Check channels that have co-stream inputs enabled whether they are still live,
    /// and if they aren't, make them leave.
    private async Task CoStreamInputsLiveWorker(CancellationToken cancellationToken)
    {
        TimeSpan delay = TimeSpan.FromMinutes(1);
        const int chunkSize = 100;

        while (!cancellationToken.IsCancellationRequested)
        {
            IImmutableSet<string> joinedChannelIds = await _coStreamChannelsRepo.GetJoinedChannels();
            TwitchAPI api = await _twitchApiProvider.Get();

            HashSet<string> liveChannelIds = [];
            foreach (string[] chunk in joinedChannelIds.Chunk(chunkSize))
            {
                GetStreamsResponse getStreamsResponse =
                    await api.Helix.Streams.GetStreamsAsync(userIds: [..chunk], first: chunkSize);
                foreach (Stream s in getStreamsResponse.Streams)
                    liveChannelIds.Add(s.UserId);
            }
            IImmutableSet<string> offlineChannelIds = joinedChannelIds.Except(liveChannelIds);
            foreach (string channelId in offlineChannelIds)
            {
                _logger.LogDebug("auto-leaving co-stream-inputs channel {Channel} because it's not live", channelId);
                await Leave(channelId);
            }

            try { await Task.Delay(delay, cancellationToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunAndReconnectForever(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                _logger.LogDebug("EventSub websocket client connecting...");
                EventSubClient.DisconnectReason disconnectReason = await _client.ConnectAndReceive(cancellationToken);
                if (disconnectReason == EventSubClient.DisconnectReason.KeepaliveTimeout)
                    _logger.LogWarning("EventSub websocket closed because of keepalive timeout, reconnecting...");
                else if (disconnectReason == EventSubClient.DisconnectReason.RemoteDisconnected)
                    _logger.LogWarning("EventSub websocket unexpectedly closed by the remote, reconnecting...");
                else
                    _logger.LogWarning("EventSub websocket closed for unknown reasons ({Reason})", disconnectReason);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("EventSub websocket client was cancelled");
                break;
            }
        }
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        List<Task> tasks = [];
        tasks.Add(RunAndReconnectForever(cancellationToken));
        if (_coStreamInputsOnlyLive)
            tasks.Add(CoStreamInputsLiveWorker(cancellationToken));
        await TaskUtils.WhenAllFastExit(tasks);
    }

    private static ImmutableList<Emote> GetEmotesFromFragments(IEnumerable<ChannelChatMessage.Fragment> fragments)
    {
        List<Emote> emotes = [];
        int currTextIndex = 0;
        foreach (ChannelChatMessage.Fragment fragment in fragments)
        {
            if (fragment.Emote is not null)
            {
                int endIndex = currTextIndex + fragment.Text.Length - 1; // -1 because the end is inclusive
                emotes.Add(new Emote(fragment.Emote.Id, fragment.Text, currTextIndex, endIndex));
            }
            currTextIndex += fragment.Text.Length;
        }
        return emotes.ToImmutableList();
    }

    private static string ConstructIrcString(ChannelChatMessage channelChatMessage, IImmutableList<Emote> emotes)
    {
        ChannelChatMessage.Event evt = channelChatMessage.Payload.Event;
        var ircTags = new Dictionary<string, string>
        {
            ["badges"] = string.Join(',', evt.Badges
                // construct irc-compatible badge string, e.g. "badges=subscriber/18,twitch-recap-2023/1"
                .Select(b => b.SetId + "/" + (string.IsNullOrEmpty(b.Info) ? "1" : b.Info))),
            ["emotes"] = string.Join('/', emotes
                // construct irc-compatible emotes string, e.g. "25:2-6,8-12/36:16-21"
                .GroupBy(e => e.Id)
                .Select(emoteGroup => emoteGroup.Key + ":" + string.Join(',', emoteGroup
                    .Select(occurrence => occurrence.StartIndex + "-" + occurrence.EndIndex)))),
            ["color"] = evt.Color,
            ["display-name"] = evt.ChatterUserName,
            ["id"] = evt.MessageId,
            ["mod"] = evt.Badges.Any(b => b.SetId is "moderator") ? "1" : "0",
            ["subscriber"] = evt.Badges.Any(b => b.SetId is "subscriber") ? "1" : "0",
            ["tmi-sent-ts"] = channelChatMessage.Metadata.MessageTimestamp.ToUnixTimeMilliseconds().ToString(),
            ["user-id"] = evt.ChatterUserId,
        };
        string tags = string.Join(';', ircTags.Select(kvp => kvp.Key + "=" + kvp.Value));
        return $"@{tags} :{evt.ChatterUserLogin}!{evt.ChatterUserLogin}@{evt.ChatterUserLogin}.tmi.twitch.tv" +
               $" PRIVMSG #{evt.BroadcasterUserLogin} :{evt.Message.Text}";
    }

    private async Task MessageReceived(ChannelChatMessage channelChatMessage)
    {
        ChannelChatMessage.Event evt = channelChatMessage.Payload.Event;
        _logger.LogDebug("<#{Channel} {Username}: {Message}",
            evt.BroadcasterUserLogin, evt.ChatterUserLogin, evt.Message.Text);
        if (evt.ChatterUserId == _userId)
            // new core sees messages posted by old core, but we don't want to process our own messages
            return;
        bool isPrimaryChat = evt.BroadcasterUserId == _channelId;
        MessageSource source = isPrimaryChat
            ? new MessageSource.PrimaryChat()
            : new MessageSource.SecondaryChat(evt.BroadcasterUserId, evt.BroadcasterUserLogin);
        User user = await _userRepo.RecordUser(new UserInfo(
            Id: evt.ChatterUserId,
            TwitchDisplayName: evt.ChatterUserName,
            SimpleName: evt.ChatterUserLogin,
            Color: string.IsNullOrEmpty(evt.Color) ? null : HexColor.FromWithHash(evt.Color),
            FromMessage: true,
            FromWhisper: false,
            UpdatedAt: channelChatMessage.Metadata.MessageTimestamp
        ));
        if (source is MessageSource.SecondaryChat secondaryChat)
        {
            if (_channelState?.EmoteMode == true)
            {
                // Emote-Only mode is often used by mods to disable inputs. We don't want this to be bypass-able.
                _logger.LogDebug(
                    "dropping input from co-stream channel {Channel} because we're in emote-only mode: {Input}",
                    secondaryChat.ChannelName, evt.Message.Text);
                return;
            }
            if (_channelState?.SlowMode != null)
            {
                // If the main channel is in slow-mode, we don't want to accept faster inputs from other channels.
                // Emulate slow-mode by dropping too fast inputs on a per-user basis.
                Instant now = _clock.GetCurrentInstant();
                Instant cutoff = now - Duration.FromSeconds(_channelState.SlowModeWaitTimeSeconds!.Value);
                if (_lastInputPerUser.TryGetValue(user, out Instant lastInput) && lastInput > cutoff)
                {
                    _logger.LogDebug("dropping too-fast input from co-stream channel {Channel}: {Input}",
                        secondaryChat.ChannelName, evt.Message.Text);
                    return;
                }
                _lastInputPerUser[user] = now;
            }
        }

        ImmutableList<Emote> emotes = GetEmotesFromFragments(evt.Message.Fragments);
        string irc = ConstructIrcString(channelChatMessage, emotes); // backwards compatibility, old-core needs this
        var message = new Message(user, evt.Message.Text, source, irc)
        {
            Details = new MessageDetails(
                MessageId: evt.MessageId,
                IsStaff: evt.Badges.Any(b => b.SetId is "moderator" or "broadcaster"),
                Emotes: emotes
            )
        };
        IncomingMessage?.Invoke(this, new MessageEventArgs(message));
    }
}
