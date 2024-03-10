using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Common;
using TPP.Core.Configuration;
using TPP.Core.Moderation;
using TPP.Core.Overlay;
using TPP.Persistence;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using OnConnectedEventArgs = TwitchLib.Client.Events.OnConnectedEventArgs;
using User = TPP.Model.User;

namespace TPP.Core.Chat
{
    public sealed class TwitchChat : IChat, IChatModeChanger, IExecutor
    {
        public string Name { get; }
        public event EventHandler<MessageEventArgs>? IncomingMessage;

        private readonly ILogger<TwitchChat> _logger;
        private readonly IClock _clock;
        private readonly string _channel;
        public readonly string ChannelId;
        private readonly bool _coStreamInputsEnabled;
        private readonly bool _coStreamInputsOnlyLive;
        private readonly IUserRepo _userRepo;
        private readonly ICoStreamChannelsRepo _coStreamChannelsRepo;
        private readonly TwitchClient _twitchClient;
        public readonly TwitchApi TwitchApi;
        private readonly TwitchLibSubscriptionWatcher? _subscriptionWatcher;
        private readonly TwitchChatSender _twitchChatSender;
        private readonly TwitchChatModeChanger _twitchChatModeChanger;
        private readonly TwitchChatExecutor _twitchChatExecutor;

        private ChannelState? _channelState;

        public enum JoinResult { Ok, NotEnabled, AlreadyJoined, UserNotFound, StreamOffline }
        public async Task<JoinResult> Join(string userLogin)
        {
            if (!_coStreamInputsEnabled)
                return JoinResult.NotEnabled;

            GetUsersResponse getUsersResponse = await TwitchApi.GetUsersAsync(logins: [userLogin]);
            var user = getUsersResponse.Users.FirstOrDefault();
            if (user == null)
                return JoinResult.UserNotFound;

            if (await _coStreamChannelsRepo.IsJoined(userLogin))
                return JoinResult.AlreadyJoined;

            if (_coStreamInputsOnlyLive)
            {
                GetStreamsResponse getStreamsResponse =
                    await TwitchApi.GetStreamsAsync(userLogins: [userLogin]);
                Stream? stream = getStreamsResponse.Streams.FirstOrDefault();
                if (stream is null)
                    return JoinResult.StreamOffline;
            }

            await _coStreamChannelsRepo.Add(userLogin, user.ProfileImageUrl);
            await _twitchClient.JoinChannelAsync(userLogin);
            await _twitchClient.SendMessageAsync(userLogin, "Joined channel, hello!");

            return JoinResult.Ok;
        }

        public enum LeaveResult { Ok, NotJoined }
        public async Task<LeaveResult> Leave(string userLogin)
        {
            if (!await _coStreamChannelsRepo.IsJoined(userLogin))
                return LeaveResult.NotJoined;
            await _twitchClient.SendMessageAsync(userLogin, "Leaving channel, goodbye!");
            await _twitchClient.LeaveChannelAsync(userLogin);
            await _coStreamChannelsRepo.Remove(userLogin);
            return LeaveResult.Ok;
        }

        public TwitchChat(
            string name,
            ILoggerFactory loggerFactory,
            IClock clock,
            ConnectionConfig.Twitch chatConfig,
            IUserRepo userRepo,
            ICoStreamChannelsRepo coStreamChannelsRepo,
            ISubscriptionProcessor subscriptionProcessor,
            OverlayConnection overlayConnection,
            bool useTwitchReplies = true)
        {
            Name = name;
            _logger = loggerFactory.CreateLogger<TwitchChat>();
            _clock = clock;
            _channel = chatConfig.Channel;
            ChannelId = chatConfig.ChannelId;
            _coStreamInputsEnabled = chatConfig.CoStreamInputsEnabled;
            _coStreamInputsOnlyLive = chatConfig.CoStreamInputsOnlyLive;
            _userRepo = userRepo;
            _coStreamChannelsRepo = coStreamChannelsRepo;

            TwitchApi = new TwitchApi(
                loggerFactory,
                clock,
                chatConfig.InfiniteAccessToken,
                chatConfig.RefreshToken,
                chatConfig.AppClientId,
                chatConfig.AppClientSecret);
            _twitchClient = new TwitchClient(
                client: new WebSocketClient(),
                loggerFactory: loggerFactory);
            var credentials = new ConnectionCredentials(
                twitchUsername: chatConfig.Username,
                twitchOAuth: chatConfig.Password,
                disableUsernameCheck: true);
            // disable TwitchLib's command features, we do that ourselves
            _twitchClient.ChatCommandIdentifiers.Add('\0');
            _twitchClient.WhisperCommandIdentifiers.Add('\0');
            _twitchClient.Initialize(
                credentials: credentials,
                channel: chatConfig.Channel);

            _twitchClient.OnConnected += Connected;
            _twitchClient.OnError += OnError;
            _twitchClient.OnConnectionError += OnConnectionError;
            _twitchClient.OnMessageReceived += MessageReceived;
            _twitchClient.OnWhisperReceived += WhisperReceived;
            _twitchClient.OnChannelStateChanged += ChannelStateChanged;
            _twitchChatSender = new TwitchChatSender(loggerFactory, TwitchApi, chatConfig, useTwitchReplies);
            _twitchChatModeChanger = new TwitchChatModeChanger(
                loggerFactory.CreateLogger<TwitchChatModeChanger>(), TwitchApi, chatConfig);
            _twitchChatExecutor = new TwitchChatExecutor(loggerFactory.CreateLogger<TwitchChatExecutor>(),
                TwitchApi, chatConfig);

            _subscriptionWatcher = chatConfig.MonitorSubscriptions
                ? new TwitchLibSubscriptionWatcher(loggerFactory, _userRepo, _twitchClient, clock,
                    subscriptionProcessor, _twitchChatSender, overlayConnection, chatConfig.Channel)
                : null;
        }

        private async Task Connected(object? sender, OnConnectedEventArgs e)
        {
            await _twitchClient.JoinChannelAsync(_channel);
            if (_coStreamInputsEnabled)
                foreach (string channel in await _coStreamChannelsRepo.GetJoinedChannels())
                    await _twitchClient.JoinChannelAsync(channel);
        }

        // Subscribe to TwitchClient errors to hopefully prevent the very rare incidents where the bot effectively
        // gets disconnected, but the CheckConnectivityWorker cannot detect it and doesn't reconnect.
        // I've never caught this event firing (it doesn't fire when you pull the ethernet cable either)
        // but the theory is that if this bug occurs: https://github.com/dotnet/runtime/issues/48246 we can call
        // Disconnect() to force the underlying ClientWebSocket.State to change to Abort.
        private async Task OnError(object? sender, OnErrorEventArgs e)
        {
            _logger.LogError(e.Exception, "The TwitchClient encountered an error. Forcing a disconnect");
            await _twitchClient.DisconnectAsync();
            // let the CheckConnectivityWorker handle reconnecting
        }

        private async Task OnConnectionError(object? sender, OnConnectionErrorArgs e)
        {
            // same procedure as above
            _logger.LogError("The TwitchClient encountered a connection error. Forcing a disconnect. Error: {Error}",
                e.Error.Message);
            await _twitchClient.DisconnectAsync();
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            await _twitchClient.ConnectAsync();
            _logger.LogInformation("Connected to Twitch, channels: {Channels}",
                string.Join(", ", _twitchClient.JoinedChannels.Select(c => c.Channel)));

            List<Task> tasks = [];
            tasks.Add(CheckConnectivityWorker(cancellationToken));
            if (_coStreamInputsOnlyLive)
                tasks.Add(CoStreamInputsLiveWorker(cancellationToken));
            // Must wait on all concurrently running tasks simultaneously to know when one of them crashed
            await Task.WhenAll(tasks);

            await _twitchClient.DisconnectAsync();
            await _twitchChatSender.DisposeAsync();
            _twitchClient.OnConnected -= Connected;
            _subscriptionWatcher?.Dispose();
            _twitchClient.OnMessageReceived -= MessageReceived;
            _twitchClient.OnWhisperReceived -= WhisperReceived;
            _twitchClient.OnChannelStateChanged -= ChannelStateChanged;
            _logger.LogDebug("twitch chat is now fully shut down");
        }

        /// TwitchClient's disconnect event appears to fire unreliably,
        /// so it is safer to manually check the connection every few seconds.
        private async Task CheckConnectivityWorker(CancellationToken cancellationToken)
        {
            TimeSpan minDelay = TimeSpan.FromSeconds(3);
            TimeSpan maxDelay = TimeSpan.FromSeconds(30);
            TimeSpan delay = minDelay;
            while (!cancellationToken.IsCancellationRequested)
            {
                delay *= _twitchClient.IsConnected ? 0.5 : 2;
                if (delay > maxDelay) delay = maxDelay;
                if (delay < minDelay) delay = minDelay;

                if (!_twitchClient.IsConnected)
                {
                    _logger.LogError("Not connected to twitch, trying to reconnect...");
                    try
                    {
                        await _twitchClient.ReconnectAsync();
                        _logger.LogInformation("Successfully reconnected to twitch.");
                    }
                    catch (Exception)
                    {
                        _logger.LogError("Failed to reconnect, trying again in {Delay} seconds", delay.TotalSeconds);
                    }
                }

                try { await Task.Delay(delay, cancellationToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        /// Check channels that have co-stream inputs enabled whether they are still live,
        /// and if they aren't, make them leave.
        private async Task CoStreamInputsLiveWorker(CancellationToken cancellationToken)
        {
            TimeSpan delay = TimeSpan.FromMinutes(1);
            const int chunkSize = 100;

            while (!cancellationToken.IsCancellationRequested)
            {
                IImmutableSet<string> joinedChannels = await _coStreamChannelsRepo.GetJoinedChannels();

                HashSet<string> liveChannels = [];
                foreach (string[] chunk in joinedChannels.Chunk(chunkSize))
                {
                    GetStreamsResponse getStreamsResponse =
                        await TwitchApi.GetStreamsAsync(userLogins: [..chunk], first: chunkSize);
                    foreach (Stream s in getStreamsResponse.Streams)
                        liveChannels.Add(s.UserLogin);
                }
                IImmutableSet<string> offlineChannels = joinedChannels.Except(liveChannels);
                foreach (string channel in offlineChannels)
                {
                    _logger.LogDebug("auto-leaving co-stream-inputs channel {Channel} because it's not live", channel);
                    await Leave(channel);
                }

                try { await Task.Delay(delay, cancellationToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        private readonly Dictionary<User, Instant> _lastInputPerUser = new();

        private async Task MessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            _logger.LogDebug("<#{Channel} {Username}: {Message}",
                _channel, e.ChatMessage.Username, e.ChatMessage.Message);
            if (e.ChatMessage.Username == _twitchClient.TwitchUsername)
                // new core sees messages posted by old core, but we don't want to process our own messages
                return;
            bool isPrimaryChat = e.ChatMessage.Channel.Equals(_channel, StringComparison.InvariantCultureIgnoreCase);
            MessageSource source = isPrimaryChat
                ? new MessageSource.PrimaryChat()
                : new MessageSource.SecondaryChat(e.ChatMessage.Channel);
            User user = await _userRepo.RecordUser(GetUserInfoFromTwitchMessage(e.ChatMessage, fromWhisper: false));
            if (source is MessageSource.SecondaryChat secondaryChat)
            {
                if (_channelState?.EmoteOnly == true)
                {
                    // Emote-Only mode is often used by mods to disable inputs. We don't want this to be bypass-able.
                    _logger.LogDebug(
                        "dropping input from co-stream channel {Channel} because we're in emote-only mode: {Input}",
                        secondaryChat.ChannelName, e.ChatMessage.Message);
                    return;
                }
                if (_channelState?.SlowMode != null)
                {
                    // If the main channel is in slow-mode, we don't want to accept faster inputs from other channels.
                    // Emulate slow-mode by dropping too fast inputs on a per-user basis.
                    Instant now = _clock.GetCurrentInstant();
                    Instant cutoff = now - Duration.FromSeconds(_channelState.SlowMode.Value);
                    if (_lastInputPerUser.TryGetValue(user, out Instant lastInput) && lastInput > cutoff)
                    {
                        _logger.LogDebug("dropping too-fast input from co-stream channel {Channel}: {Input}",
                            secondaryChat.ChannelName, e.ChatMessage.Message);
                        return;
                    }
                    _lastInputPerUser[user] = now;
                }
            }
            var message = new Message(user, e.ChatMessage.Message, source, e.ChatMessage.RawIrcMessage)
            {
                Details = new MessageDetails(
                    MessageId: e.ChatMessage.Id,
                    IsAction: e.ChatMessage.IsMe,
                    IsStaff: e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator,
                    Emotes: e.ChatMessage.EmoteSet.Emotes
                        .Select(em => new Emote(em.Id, em.Name, em.StartIndex, em.EndIndex)).ToImmutableList()
                )
            };
            IncomingMessage?.Invoke(this, new MessageEventArgs(message));
        }

        private async Task ChannelStateChanged(object? sender, OnChannelStateChangedArgs e)
        {
            if (!e.Channel.Equals(_channel, StringComparison.InvariantCultureIgnoreCase)) return;
            _channelState = e.ChannelState;
            await Task.CompletedTask;
        }

        private async Task WhisperReceived(object? sender, OnWhisperReceivedArgs e)
        {
            _logger.LogDebug("<@{Username}: {Message}", e.WhisperMessage.Username, e.WhisperMessage.Message);
            User user = await _userRepo.RecordUser(
                GetUserInfoFromTwitchMessage(e.WhisperMessage, fromWhisper: true));
            var message = new Message(user, e.WhisperMessage.Message, new MessageSource.Whisper(),
                e.WhisperMessage.RawIrcMessage)
            {
                Details = new MessageDetails(
                    MessageId: null,
                    IsAction: false,
                    IsStaff: false,
                    Emotes: e.WhisperMessage.EmoteSet.Emotes
                        .Select(em => new Emote(em.Id, em.Name, em.StartIndex, em.EndIndex)).ToImmutableList()
                )
            };
            IncomingMessage?.Invoke(this, new MessageEventArgs(message));
        }

        private UserInfo GetUserInfoFromTwitchMessage(TwitchLibMessage message, bool fromWhisper)
        {
            string colorHex = message.HexColor;
            return new UserInfo(
                Id: message.UserId,
                TwitchDisplayName: message.DisplayName,
                SimpleName: message.Username,
                Color: string.IsNullOrEmpty(colorHex) ? null : HexColor.FromWithHash(colorHex),
                FromMessage: true,
                FromWhisper: fromWhisper,
                UpdatedAt: _clock.GetCurrentInstant()
            );
        }

        public Task EnableEmoteOnly() => _twitchChatModeChanger.EnableEmoteOnly();
        public Task DisableEmoteOnly() => _twitchChatModeChanger.DisableEmoteOnly();

        public Task DeleteMessage(string messageId) => _twitchChatExecutor.DeleteMessage(messageId);
        public Task Timeout(User user, string? message, Duration duration) =>
            _twitchChatExecutor.Timeout(user, message, duration);
        public Task Ban(User user, string? message) => _twitchChatExecutor.Ban(user, message);
        public Task Unban(User user, string? message) => _twitchChatExecutor.Unban(user, message);

        public Task SendMessage(string message, Message? responseTo = null) =>
            _twitchChatSender.SendMessage(message, responseTo);
        public Task SendWhisper(User target, string message) =>
            _twitchChatSender.SendWhisper(target, message);
    }
}
