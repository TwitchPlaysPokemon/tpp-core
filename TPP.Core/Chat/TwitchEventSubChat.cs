using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Common;
using TPP.Common.Utils;
using TPP.Core.Overlay;
using TPP.Core.Overlay.Events;
using TPP.Core.Overlay.Events.Common;
using TPP.Core.Utils;
using TPP.Persistence;
using TPP.Twitch.EventSub;
using TPP.Twitch.EventSub.Notifications;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using static TPP.Core.EventUtils;
using User = TPP.Model.User;

namespace TPP.Core.Chat;

public partial class TwitchEventSubChat : IWithLifecycle, IMessageSource
{
    public event EventHandler<MessageEventArgs>? IncomingMessage;

    private readonly ILogger<TwitchEventSubChat> _logger;
    private readonly TwitchApi _twitchApi;
    private readonly IClock _clock;
    private readonly IUserRepo _userRepo;
    private readonly ISubscriptionProcessor _subscriptionProcessor;
    private readonly OverlayConnection _overlayConnection;
    private readonly IMessageSender _responseSender;

    private readonly bool _coStreamInputsEnabled;
    private readonly bool _coStreamInputsOnlyLive;
    private readonly ICoStreamChannelsRepo _coStreamChannelsRepo;
    private readonly Dictionary<string, string> _coStreamEventSubSubscriptions = new();

    private readonly EventSubClient _clientChatBot;
    private readonly EventSubClient _clientChannel;
    private readonly string _channelId;
    private readonly string _userId;

    private ChannelChatSettingsUpdate.Event? _channelState;
    private readonly Dictionary<User, Instant> _lastInputPerUser = new();
    private Session? _session;

    public TwitchEventSubChat(
        ILoggerFactory loggerFactory,
        IClock clock,
        TwitchApi twitchApi,
        IUserRepo userRepo,
        ISubscriptionProcessor subscriptionProcessor,
        OverlayConnection overlayConnection,
        IMessageSender responseSender,
        string channelId,
        string userId,
        bool coStreamInputsEnabled,
        bool coStreamInputsOnlyLive,
        ICoStreamChannelsRepo coStreamChannelsRepo)
    {
        _logger = loggerFactory.CreateLogger<TwitchEventSubChat>();
        _twitchApi = twitchApi;
        _clock = clock;
        _userRepo = userRepo;
        _subscriptionProcessor = subscriptionProcessor;
        _overlayConnection = overlayConnection;
        _responseSender = responseSender;
        _channelId = channelId;
        _userId = userId;
        _coStreamInputsEnabled = coStreamInputsEnabled;
        _coStreamInputsOnlyLive = coStreamInputsOnlyLive;
        _coStreamChannelsRepo = coStreamChannelsRepo;

        _clientChatBot = new EventSubClient(loggerFactory, clock);
        _clientChannel = new EventSubClient(loggerFactory, clock);
        _clientChatBot.RevocationReceived += (_, revocation) =>
            _logger.LogError("received revocation for {SubscriptionType}: {Data}",
                revocation.Metadata.SubscriptionType,
                JsonSerializer.Serialize(revocation.Payload.Subscription.Condition));
        _clientChannel.RevocationReceived += (_, revocation) =>
            _logger.LogError("received revocation for {SubscriptionType}: {Data}",
                revocation.Metadata.SubscriptionType,
                JsonSerializer.Serialize(revocation.Payload.Subscription.Condition));
        _clientChatBot.NotificationReceived += (_, notification) =>
            TaskToVoidSafely(_logger, async () =>
            {
                if (notification is ChannelChatMessage chatMessage)
                    await MessageReceived(chatMessage);
                else if (notification is UserWhisperMessage whisperMessage)
                    await WhisperReceived(whisperMessage);
                else if (notification is ChannelChatSettingsUpdate settingsUpdate)
                    _channelState = settingsUpdate.Payload.Event;
                else
                    _logger.LogWarning("received unhandled bot EventSub notification of type {Type}, payload: {Payload}",
                        notification.Metadata.SubscriptionType, notification.Payload);
            });
        _clientChannel.NotificationReceived += (_, notification) =>
            TaskToVoidSafely(_logger, async () =>
            {
                _logger.LogDebug("received channel EventSub notification of noteworthy type {Type}, payload: {Payload}",
                    notification.Metadata.SubscriptionType, notification.Payload);

                if (notification is ChannelSubscribe channelSubscribe)
                    await ChannelSubscribeReceived(channelSubscribe); // new subscriptions only
                else if (notification is ChannelSubscriptionMessage channelSubscriptionMessage)
                    await ChannelSubscriptionMessageReceived(channelSubscriptionMessage); // resubscriptions only
                else if (notification is ChannelSubscriptionGift channelSubscriptionGift)
                    await ChannelSubscriptionGiftReceived(channelSubscriptionGift);
                else
                    _logger.LogWarning("received unhandled channel EventSub notification of type {Type}, payload: {Payload}",
                        notification.Metadata.SubscriptionType, notification.Payload);
            });
        _clientChatBot.Connected += (_, welcome) =>
            TaskToVoidSafely(_logger, () => SetUpChatBotSubscriptions(welcome.Payload.Session));
        _clientChannel.Connected += (_, welcome) =>
            TaskToVoidSafely(_logger, () => SetUpChannelSubscriptions(welcome.Payload.Session));
    }

    private async Task SetUpChatBotSubscriptions(Session session)
    {
        _session = session;
        _logger.LogDebug("Setting up chat bot EventSub subscriptions");
        await Task.WhenAll(
            _twitchApi.SubscribeToEventSubBot<ChannelChatMessage>(session.Id,
                new ChannelChatMessage.Condition(BroadcasterUserId: _channelId, UserId: _userId).AsDict()),
            _twitchApi.SubscribeToEventSubBot<UserWhisperMessage>(session.Id,
                new UserWhisperMessage.Condition(UserId: _userId).AsDict()),
            _twitchApi.SubscribeToEventSubBot<ChannelChatSettingsUpdate>(session.Id,
                new ChannelChatSettingsUpdate.Condition(BroadcasterUserId: _channelId, UserId: _userId).AsDict())
        );

        if (_coStreamInputsEnabled)
        {
            foreach (string channelId in await _coStreamChannelsRepo.GetJoinedChannels())
            {
                try
                {
                    var response = await _twitchApi.SubscribeToEventSubBot<ChannelChatMessage>(session.Id,
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
        _logger.LogDebug("Finished setting up chat bot EventSub subscriptions");
    }

    private async Task SetUpChannelSubscriptions(Session session)
    {
        _session = session;
        _logger.LogDebug("Setting up channel EventSub subscriptions");
        await Task.WhenAll(
            _twitchApi.SubscribeToEventSubChannel<ChannelSubscribe>(session.Id,
                new ChannelSubscribe.Condition(BroadcasterUserId: _channelId).AsDict()),
            _twitchApi.SubscribeToEventSubChannel<ChannelSubscriptionMessage>(session.Id,
                new ChannelSubscriptionMessage.Condition(BroadcasterUserId: _channelId).AsDict()),
            _twitchApi.SubscribeToEventSubChannel<ChannelSubscriptionGift>(session.Id,
                new ChannelSubscriptionGift.Condition(BroadcasterUserId: _channelId).AsDict())
        );
        _logger.LogDebug("Finished setting up channel EventSub subscriptions");
    }

    public enum JoinResult { Ok, NotEnabled, AlreadyJoined, UserNotFound, StreamOffline }
    public async Task<JoinResult> Join(string userId)
    {
        if (!_coStreamInputsEnabled)
            return JoinResult.NotEnabled;

        GetUsersResponse getUsersResponse = await _twitchApi.GetUsersAsync(ids: [userId]);
        var user = getUsersResponse.Users.FirstOrDefault();
        if (user == null)
            return JoinResult.UserNotFound;

        if (await _coStreamChannelsRepo.IsJoined(userId))
            return JoinResult.AlreadyJoined;

        if (_coStreamInputsOnlyLive)
        {
            GetStreamsResponse getStreamsResponse =
                await _twitchApi.GetStreamsAsync(userIds: [userId]);
            Stream? stream = getStreamsResponse.Streams.FirstOrDefault();
            if (stream is null)
                return JoinResult.StreamOffline;
        }

        await _coStreamChannelsRepo.Add(userId, user.ProfileImageUrl);
        var response = await _twitchApi.SubscribeToEventSubBot<ChannelChatMessage>(_session!.Id,
            new ChannelChatMessage.Condition(BroadcasterUserId: userId, UserId: _userId).AsDict());
        _coStreamEventSubSubscriptions[userId] = response.Subscriptions[0].Id;
        await _twitchApi.SendChatMessage(userId, _userId, "Joined channel, hello!");

        return JoinResult.Ok;
    }

    public enum LeaveResult { Ok, NotJoined }
    public async Task<LeaveResult> Leave(string userId)
    {
        if (!await _coStreamChannelsRepo.IsJoined(userId))
            return LeaveResult.NotJoined;
        try
        {
            await _twitchApi.SendChatMessage(userId, _userId, "Leaving channel, goodbye!");
        }
        catch (HttpResponseException ex)
        {
            _logger.LogError(ex, "Failed sending goodbye message to {ChannelID} after leaving, ignoring", userId);
        }
        if (_coStreamEventSubSubscriptions.TryGetValue(userId, out string? subscriptionId))
            await _twitchApi.DeleteEventSubSubscriptionAsyncBot(subscriptionId);
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

            HashSet<string> liveChannelIds = [];
            foreach (string[] chunk in joinedChannelIds.Chunk(chunkSize))
            {
                GetStreamsResponse getStreamsResponse =
                    await _twitchApi.GetStreamsAsync(userIds: [..chunk], first: chunkSize);
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

    private readonly Duration _minThrottle = Duration.FromSeconds(1);
    private readonly Duration _maxThrottle = Duration.FromSeconds(30);
    private readonly TtlCount _recentConnectAttempts = new(Duration.FromMinutes(10), SystemClock.Instance);
    private async Task RunAndReconnectForever(CancellationToken cancellationToken)
    {
        Task<EventSubClient.DisconnectReason>? chatBotTask = null;
        Task<EventSubClient.DisconnectReason>? channelTask = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            int throttleSteps = _recentConnectAttempts.Count - 1; // -1 so first reconnect is not throttled
            Duration throttle = throttleSteps > 0
                ? _minThrottle * Math.Pow(2, throttleSteps - 1)
                : Duration.Zero;
            if (throttle > _maxThrottle) throttle = _maxThrottle;
            if (throttle > Duration.Zero)
            {
                _logger.LogWarning("Connecting to EventSub throttled, waiting {Duration}s...", throttle.TotalSeconds);
                await Task.Delay(throttle.ToTimeSpan(), cancellationToken);
            }
            _recentConnectAttempts.Increment();
            try
            {
                _logger.LogDebug("EventSub websocket client connecting...");
                if (chatBotTask is null or { IsCompleted: true })
                    chatBotTask = _clientChatBot.ConnectAndReceive(cancellationToken);
                if (channelTask is null or { IsCompleted: true })
                    channelTask = _clientChannel.ConnectAndReceive(cancellationToken);
                EventSubClient.DisconnectReason disconnectReason = await await Task.WhenAny(chatBotTask, channelTask);
                if (disconnectReason is EventSubClient.DisconnectReason.KeepaliveTimeout)
                    _logger.LogWarning("EventSub websocket closed because of keepalive timeout, reconnecting...");
                else if (disconnectReason is EventSubClient.DisconnectReason.ConnectionClosed)
                    _logger.LogWarning("EventSub tcp connection unexpectedly closed, reconnecting...");
                else if (disconnectReason is EventSubClient.DisconnectReason.WebsocketClosed(var status, var desc))
                    _logger.LogWarning("EventSub websocket unexpectedly closed by the remote with status {Status} " +
                                       "and description '{Description}', reconnecting...", status, desc);
                else
                    _logger.LogWarning("EventSub websocket closed for unknown reasons ({Reason})", disconnectReason);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("EventSub websocket client was cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in EventSub client, reconnecting...");
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

    private static string ConstructPrivmsgIrcString(ChannelChatMessage channelChatMessage, IImmutableList<Emote> emotes)
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

    // "/me"-messages are enveloped IRC-style using Start-of-Heading (SOH=\u0001) characters and "ACTION" prefix
    [GeneratedRegex(@"^\u0001ACTION (.*)\u0001$", RegexOptions.Compiled)]
    private static partial Regex MeMessageRegex();
    private async Task MessageReceived(ChannelChatMessage channelChatMessage)
    {
        ChannelChatMessage.Event evt = channelChatMessage.Payload.Event;
        string cleanMessageText = evt.Message.Text;
        bool isAction = false;
        if (MeMessageRegex().Match(cleanMessageText) is { Groups: [_, var envelopeContent] })
        {
            cleanMessageText = envelopeContent.Value;
            isAction = true;
        }
        _logger.LogDebug("<#{Channel} {Username}: {Message}",
            evt.BroadcasterUserLogin, evt.ChatterUserLogin, cleanMessageText);
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
                    secondaryChat.ChannelName, cleanMessageText);
                return;
            }
            if (_channelState?.SlowMode == true)
            {
                // If the main channel is in slow-mode, we don't want to accept faster inputs from other channels.
                // Emulate slow-mode by dropping too fast inputs on a per-user basis.
                Instant now = _clock.GetCurrentInstant();
                Instant cutoff = now - Duration.FromSeconds(_channelState.SlowModeWaitTimeSeconds!.Value);
                if (_lastInputPerUser.TryGetValue(user, out Instant lastInput) && lastInput > cutoff)
                {
                    _logger.LogDebug("dropping too-fast input from co-stream channel {Channel}: {Input}",
                        secondaryChat.ChannelName, cleanMessageText);
                    return;
                }
                _lastInputPerUser[user] = now;
            }
        }

        ImmutableList<Emote> emotes = GetEmotesFromFragments(evt.Message.Fragments);
        string irc = ConstructPrivmsgIrcString(channelChatMessage, emotes); // backwards compatibility, old-core needs this
        var message = new Message(user, cleanMessageText, source, irc)
        {
            Details = new MessageDetails(
                MessageId: evt.MessageId,
                IsAction: isAction,
                IsStaff: evt.Badges.Any(b => b.SetId is "moderator" or "broadcaster"),
                Emotes: emotes
            )
        };
        IncomingMessage?.Invoke(this, new MessageEventArgs(message));
    }

    private static string ConstructWhisperIrcString(UserWhisperMessage whisperMessage)
    {
        UserWhisperMessage.Event evt = whisperMessage.Payload.Event;
        var ircTags = new Dictionary<string, string>
        {
            ["display-name"] = evt.FromUserName,
            ["user-id"] = evt.FromUserId,
            ["message-id"] = evt.WhisperId,
        };
        string tags = string.Join(';', ircTags.Select(kvp => kvp.Key + "=" + kvp.Value));
        return $"@{tags} :{evt.FromUserLogin}!{evt.FromUserLogin}@{evt.FromUserLogin}.tmi.twitch.tv" +
               $" WHISPER {evt.ToUserLogin} :{evt.Whisper.Text}";
    }

    private async Task WhisperReceived(UserWhisperMessage whisperMessage)
    {
        UserWhisperMessage.Event whisperEvent = whisperMessage.Payload.Event;
        _logger.LogDebug("<@{Username}: {Message}", whisperEvent.FromUserLogin, whisperEvent.Whisper.Text);
        User user = await _userRepo.RecordUser(new UserInfo(
            Id: whisperEvent.FromUserId,
            TwitchDisplayName: whisperEvent.FromUserName,
            SimpleName: whisperEvent.FromUserLogin,
            FromMessage: true,
            FromWhisper: true,
            UpdatedAt: whisperMessage.Metadata.MessageTimestamp
        ));
        var message = new Message(user, whisperEvent.Whisper.Text, new MessageSource.Whisper(),
            ConstructWhisperIrcString(whisperMessage))
        {
            Details = new MessageDetails(
                MessageId: null,
                IsAction: false,
                IsStaff: false,
                Emotes: []
            )
        };
        IncomingMessage?.Invoke(this, new MessageEventArgs(message));
    }

    private static SubscriptionTier ParseTier(ChannelSubscribe.Tier tier) =>
        tier switch
        {
            ChannelSubscribe.Tier.Tier1000 => SubscriptionTier.Tier1,
            ChannelSubscribe.Tier.Tier2000 => SubscriptionTier.Tier2,
            ChannelSubscribe.Tier.Tier3000 => SubscriptionTier.Tier3,
        };

    private async Task ChannelSubscribeReceived(ChannelSubscribe channelSubscribe)
    {
        ChannelSubscribe.Event evt = channelSubscribe.Payload.Event;
        Debug.Assert(evt.BroadcasterUserId == _channelId, "Must only process subs of own channel");
        User subscriber = await _userRepo.RecordUser(new UserInfo(
            Id: evt.UserId,
            TwitchDisplayName: evt.UserName,
            SimpleName: evt.UserLogin,
            UpdatedAt: channelSubscribe.Metadata.MessageTimestamp
        ));
        // If a user subscribes, then cancels, and later subscribes again, I believe we get this message.
        // This event does not tell us the cumulative months, so let's just guess using our previous knowledge.
        int cumulativeMonths = subscriber.MonthsSubscribed + 1;
        SubscriptionInfo subscriptionInfo = new(
            Subscriber: subscriber,
            NumMonths: cumulativeMonths,
            StreakMonths: null, // this is a new subscription, hence no streak
            Tier: ParseTier(evt.Tier),
            PlanName: null, // EventSub does not give us the informational plan name (like "Channel Subscription: $24.99 Sub")
            SubscriptionAt: channelSubscribe.Metadata.MessageTimestamp,
            IsGift: evt.IsGift,
            Message: null, // Only the "subscription message event" (resubs) has user defined messages
            Emotes: []
        );
        ISubscriptionProcessor.SubResult subResult = await _subscriptionProcessor
            .ProcessSubscription(subscriptionInfo);

        string response = BuildSubResponse(subResult, null, false);
        await _responseSender.SendWhisper(subscriptionInfo.Subscriber, response);
        await _overlayConnection.Send(new NewSubscriber
        {
            User = subscriptionInfo.Subscriber,
            Emotes = subscriptionInfo.Emotes.Select(EmoteInfo.FromOccurence).ToImmutableList(),
            SubMessage = subscriptionInfo.Message,
            ShareSub = true,
        }, CancellationToken.None);
    }

    private async Task ChannelSubscriptionMessageReceived(ChannelSubscriptionMessage channelSubscriptionMessage)
    {
        ChannelSubscriptionMessage.Event evt = channelSubscriptionMessage.Payload.Event;
        Debug.Assert(evt.BroadcasterUserId == _channelId, "Must only process subs of own channel");
        User subscriber = await _userRepo.RecordUser(new UserInfo(
            Id: evt.UserId,
            TwitchDisplayName: evt.UserName,
            SimpleName: evt.UserLogin,
            UpdatedAt: channelSubscriptionMessage.Metadata.MessageTimestamp
        ));
        SubscriptionInfo subscriptionInfo = new(
            Subscriber: subscriber,
            NumMonths: evt.CumulativeMonths,
            StreakMonths: evt.StreakMonths,
            Tier: ParseTier(evt.Tier),
            PlanName: null, // EventSub does not give us the informational plan name (like "Channel Subscription: $24.99 Sub")
            SubscriptionAt: channelSubscriptionMessage.Metadata.MessageTimestamp,
            IsGift: false, // Resubscriptions are never gifts. Gifts are always "new" subscriptions, not continuations
            Message: evt.Message?.Text,
            Emotes: (evt.Message?.Emotes ?? []).Select(e => new EmoteOccurrence(
                    e.Id, evt.Message!.Text!.Substring(e.Begin, e.End - e.Begin), e.Begin, e.End))
                .ToImmutableList()
        );
        ISubscriptionProcessor.SubResult subResult = await _subscriptionProcessor.ProcessSubscription(
            subscriptionInfo);

        string response = BuildSubResponse(subResult, null, false);
        await _responseSender.SendWhisper(subscriptionInfo.Subscriber, response);

        await _overlayConnection.Send(new NewSubscriber
        {
            User = subscriptionInfo.Subscriber,
            Emotes = subscriptionInfo.Emotes.Select(EmoteInfo.FromOccurence).ToImmutableList(),
            SubMessage = subscriptionInfo.Message,
            ShareSub = true,
        }, CancellationToken.None);
    }

    private async Task ChannelSubscriptionGiftReceived(ChannelSubscriptionGift channelSubscriptionGift)
    {
        ChannelSubscriptionGift.Event evt = channelSubscriptionGift.Payload.Event;
        Debug.Assert(evt.BroadcasterUserId == _channelId, "Must only process subs of own channel");
        if (evt.IsAnonymous)
        {
            _logger.LogDebug("Skipping processing of sub gift event because gifter is anonymous");
            return;
        }

        int? cumulativeTotal = evt.CumulativeTotal; // TODO use?
        User gifter = await _userRepo.RecordUser(new UserInfo(
            Id: evt.UserId!,
            TwitchDisplayName: evt.UserName!,
            SimpleName: evt.UserLogin!,
            UpdatedAt: channelSubscriptionGift.Metadata.MessageTimestamp
        ));
        SubscriptionGiftInfo subscriptionGiftInfo = new(
            Gifter: gifter,
            Tier: ParseTier(evt.Tier),
            SubscriptionGiftAt: channelSubscriptionGift.Metadata.MessageTimestamp,
            NumGifts: evt.Total
        );
        ISubscriptionProcessor.SubGiftResult subGiftResult = await _subscriptionProcessor.ProcessSubscriptionGift(
            subscriptionGiftInfo);
        string subGiftResponse = subGiftResult switch
        {
            ISubscriptionProcessor.SubGiftResult.OkButLinked {GifterTokens: 0, LinkedUsers: [var linked]}  =>
                $"As you are linked to the account '{linked.Name}' you have gifted to, " +
                $"you have not received a token bonus. " +
                "The recipient account still gains the normal benefits however. Thanks for subscribing!",
            ISubscriptionProcessor.SubGiftResult.OkButLinked {GifterTokens: 0, LinkedUsers: var linked}  =>
                $"As you are linked to the accounts you have gifted to " +
                $"({string.Join(", ", linked.Select(u => u.Name))}), you have not received a token bonus. " +
                "The recipient accounts still gain the normal benefits however. Thanks for subscribing!",
            ISubscriptionProcessor.SubGiftResult.OkButLinked {GifterTokens: var tokens, LinkedUsers: var linked} =>
                $"Thank you for your generosity! You received T{tokens} tokens for giving a gift subscription. " +
                $"Note that as you are linked to some accounts you have gifted to " +
                $"({string.Join(", ", linked.Select(u => u.Name))}), you have received a smaller token bonus. " +
                "The recipient accounts still gain the normal benefits however. Thanks for subscribing!",
            ISubscriptionProcessor.SubGiftResult.Ok { GifterTokens: var tokens, NumGifts: 1 } =>
                $"Thank you for your generosity! You received T{tokens} tokens for giving a gift subscription. " +
                "The recipient has been notified and awarded their token benefits.",
            ISubscriptionProcessor.SubGiftResult.Ok { GifterTokens: var tokens } =>
                $"Thank you for your generosity! You received T{tokens} tokens for giving some gift subscriptions. " +
                "The recipients have been notified and awarded their token benefits.",
            _ => throw new ArgumentOutOfRangeException(nameof(subGiftResult))
        };
        await _responseSender.SendWhisper(subscriptionGiftInfo.Gifter, subGiftResponse);
    }

    private static string BuildSubResponse(
        ISubscriptionProcessor.SubResult subResult, User? gifter, bool isAnonymous)
    {
        return subResult switch
        {
            ISubscriptionProcessor.SubResult.Ok ok => BuildOkMessage(ok, gifter, isAnonymous),
            ISubscriptionProcessor.SubResult.SameMonth sameMonth =>
                $"We detected that you've already announced your resub for month {sameMonth.Month}, " +
                "and received the appropriate tokens. " +
                "If you believe this is in error, please contact a moderator so this can be corrected.",
            _ => throw new ArgumentOutOfRangeException(nameof(subResult)),
        };

        static string BuildOkMessage(ISubscriptionProcessor.SubResult.Ok ok, User? gifter, bool isAnonymous)
        {
            string message = "";
            if (ok.SubCountCorrected)
                message +=
                    $"We detected that the amount of months subscribed ({ok.CumulativeMonths}) is lower than " +
                    "our system expected. This happened due to erroneously detected subscriptions in the past. " +
                    "Your account data has been adjusted accordingly, and you will receive your rewards normally. ";
            if (ok.NewLoyaltyLeague > ok.OldLoyaltyLeague)
                message += $"You reached Loyalty League {ok.NewLoyaltyLeague}! ";
            if (ok.DeltaTokens > 0)
                message += $"You gained T{ok.DeltaTokens} tokens! ";
            if (gifter != null && isAnonymous)
                message += "An anonymous user gifted you a subscription!";
            else if (gifter != null && !isAnonymous)
                message += $"{gifter.Name} gifted you a subscription!";
            else if (ok.CumulativeMonths > 1)
                message += "Thank you for resubscribing!";
            else
                message += "Thank you for subscribing!";
            return message;
        }
    }
}
