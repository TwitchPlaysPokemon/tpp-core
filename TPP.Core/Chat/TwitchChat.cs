using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Configuration;
using TPP.Core.Moderation;
using TPP.Core.Overlay;
using TPP.Core.Utils;
using TPP.Persistence;
using TwitchLib.Api.Auth;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using User = TPP.Model.User;

namespace TPP.Core.Chat
{
    public sealed class TwitchChat : IChat, IChatModeChanger, IExecutor
    {
        public string Name { get; }
        public event EventHandler<MessageEventArgs>? IncomingMessage;

        private readonly ILogger<TwitchChat> _logger;
        private readonly IClock _clock;
        public readonly string ChannelId;
        private readonly IUserRepo _userRepo;
        private readonly TwitchClient _twitchClient;
        public readonly TwitchApi TwitchApi;
        private readonly TwitchLibSubscriptionWatcher? _subscriptionWatcher;
        private readonly TwitchChatSender _twitchChatSender;
        private readonly TwitchChatModeChanger _twitchChatModeChanger;
        private readonly TwitchChatExecutor _twitchChatExecutor;
        public TwitchEventSubChat TwitchEventSubChat { get; }

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
            ChannelId = chatConfig.ChannelId;
            _userRepo = userRepo;

            TwitchApi = new TwitchApi(
                loggerFactory,
                clock,
                chatConfig.InfiniteAccessToken,
                chatConfig.RefreshToken,
                chatConfig.AppClientId,
                chatConfig.AppClientSecret);
            TwitchEventSubChat = new TwitchEventSubChat(loggerFactory, clock, TwitchApi, _userRepo,
                chatConfig.ChannelId, chatConfig.UserId,
                chatConfig.CoStreamInputsEnabled, chatConfig.CoStreamInputsOnlyLive, coStreamChannelsRepo);
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

            _twitchClient.OnError += OnError;
            _twitchClient.OnConnectionError += OnConnectionError;
            TwitchEventSubChat.IncomingMessage += MessageReceived;
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

        private void MessageReceived(object? sender, MessageEventArgs args)
        {
            IncomingMessage?.Invoke(this, args);
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

        /// Copied from TPP.Core's README.md
        private static readonly Dictionary<string, string> ScopesAndWhatTheyAreNeededFor = new()
        {
            ["chat:read"] = "Read messages from chat (via IRC/TMI)",
            ["chat:edit"] = "Send messages to chat (via IRC/TMI)",
            ["user:bot"] = "Appear in chat as bot",
            ["user:read:chat"] = "Read messages from chat. (via EventSub)",
            ["user:write:chat"] = "Send messages to chat. (via Twitch API)",
            ["user:manage:whispers"] = "Sending and receiving whispers",
            ["moderator:read:chatters"] = "Read the chatters list in the channel (e.g. for badge drops)",
            ["moderator:read:followers"] = "Read the followers list (currently old core)",
            ["moderator:manage:banned_users"] = "Timeout, ban and unban users (tpp automod, mod commands)",
            ["moderator:manage:chat_messages"] = "Delete chat messages (tpp automod, purge invalid bets)",
            ["moderator:manage:chat_settings"] = "Change chat settings, e.g. emote-only mode (mod commands)",
            ["channel:read:subscriptions"] = "Reacting to incoming subscriptions",
        };

        private void ValidateScopes(HashSet<string> presentScopes)
        {
            foreach ((string scope, string neededFor) in ScopesAndWhatTheyAreNeededFor)
                if (!presentScopes.Contains(scope))
                    _logger.LogWarning("Missing Twitch-API scope '{Scope}', needed for: {NeededFor}", scope, neededFor);
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Validating API access token...");
            ValidateAccessTokenResponse validateResult = await TwitchApi.Validate();
            _logger.LogInformation(
                "Successfully validated Twitch API access token! Client-ID: {ClientID}, User-ID: {UserID}, " +
                "Login: {Login}, Expires in: {Expires}s, Scopes: {Scopes}", validateResult.ClientId,
                validateResult.UserId, validateResult.Login, validateResult.ExpiresIn, validateResult.Scopes);
            ValidateScopes(validateResult.Scopes.ToHashSet());

            await _twitchClient.ConnectAsync();
            _logger.LogInformation("Connected to Twitch, channels: {Channels}",
                string.Join(", ", _twitchClient.JoinedChannels.Select(c => c.Channel)));

            List<Task> tasks = [];
            tasks.Add(CheckConnectivityWorker(cancellationToken));
            tasks.Add(TwitchEventSubChat.Start(cancellationToken));
            await TaskUtils.WhenAllFastExit(tasks);

            await _twitchClient.DisconnectAsync();
            await _twitchChatSender.DisposeAsync();
            _subscriptionWatcher?.Dispose();
            TwitchEventSubChat.IncomingMessage -= MessageReceived;
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
