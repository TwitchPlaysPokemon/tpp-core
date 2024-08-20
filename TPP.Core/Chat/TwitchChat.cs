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
using User = TPP.Model.User;

namespace TPP.Core.Chat
{
    public sealed class TwitchChat : IChat, IChatModeChanger, IExecutor
    {
        public string Name { get; }
        public event EventHandler<MessageEventArgs>? IncomingMessage;

        private readonly ILogger<TwitchChat> _logger;
        public readonly string ChannelId;
        public readonly TwitchApi TwitchApi;
        private readonly string _channelName;
        private readonly string _botUsername;
        private readonly string _appClientId;
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
            ChannelId = chatConfig.ChannelId;
            _channelName = chatConfig.Channel;
            _botUsername = chatConfig.Username;
            _appClientId = chatConfig.AppClientId;

            TwitchApi = new TwitchApi(
                loggerFactory,
                clock,
                chatConfig.InfiniteAccessToken,
                chatConfig.RefreshToken,
                chatConfig.ChannelInfiniteAccessToken,
                chatConfig.ChannelRefreshToken,
                chatConfig.AppClientId,
                chatConfig.AppClientSecret);
            _twitchChatSender = new TwitchChatSender(loggerFactory, TwitchApi, chatConfig, useTwitchReplies);
            TwitchEventSubChat = new TwitchEventSubChat(loggerFactory, clock, TwitchApi, userRepo,
                subscriptionProcessor, overlayConnection, _twitchChatSender,
                chatConfig.ChannelId, chatConfig.UserId,
                chatConfig.CoStreamInputsEnabled, chatConfig.CoStreamInputsOnlyLive, coStreamChannelsRepo);

            TwitchEventSubChat.IncomingMessage += MessageReceived;
            _twitchChatModeChanger = new TwitchChatModeChanger(
                loggerFactory.CreateLogger<TwitchChatModeChanger>(), TwitchApi, chatConfig);
            _twitchChatExecutor = new TwitchChatExecutor(loggerFactory.CreateLogger<TwitchChatExecutor>(),
                TwitchApi, chatConfig);
        }

        private void MessageReceived(object? sender, MessageEventArgs args)
        {
            IncomingMessage?.Invoke(this, args);
        }

        private enum ScopeType { Bot, Channel, Both }
        private record ScopeInfo(string Scope, string NeededFor, ScopeType ScopeType);
        /// Mostly copied from TPP.Core's README.md
        private static readonly List<ScopeInfo> ScopeInfos =
        [
            new ScopeInfo("chat:read", "Read messages from chat (via IRC/TMI)", ScopeType.Bot),
            new ScopeInfo("chat:edit", "Send messages to chat (via IRC/TMI)", ScopeType.Bot),
            new ScopeInfo("user:bot", "Appear in chat as bot", ScopeType.Bot),
            new ScopeInfo("user:read:chat", "Read messages from chat. (via EventSub)", ScopeType.Bot),
            new ScopeInfo("user:write:chat", "Send messages to chat. (via Twitch API)", ScopeType.Bot),
            new ScopeInfo("user:manage:whispers", "Sending and receiving whispers", ScopeType.Bot),
            new ScopeInfo("moderator:read:chatters", "Read the chatters list in the channel (e.g. for badge drops)",
                ScopeType.Bot),
            new ScopeInfo("moderator:read:followers", "Read the followers list (currently old core)", ScopeType.Bot),
            new ScopeInfo("moderator:manage:banned_users", "Timeout, ban and unban users (tpp automod, mod commands)",
                ScopeType.Bot),
            new ScopeInfo("moderator:manage:chat_messages", "Delete chat messages (tpp automod, purge invalid bets)",
                ScopeType.Bot),
            new ScopeInfo("moderator:manage:chat_settings", "Change chat settings, e.g. emote-only mode (mod commands)",
                ScopeType.Bot),
            new ScopeInfo("channel:read:subscriptions", "Reacting to incoming subscriptions", ScopeType.Channel)
        ];
        private static readonly Dictionary<string, ScopeInfo> ScopeInfosPerScope = ScopeInfos
            .ToDictionary(scopeInfo => scopeInfo.Scope, scopeInfo => scopeInfo);

        private async Task ValidateApiTokens()
        {
            _logger.LogDebug("Validating API access token...");
            ValidateAccessTokenResponse botTokenInfo = await TwitchApi.ValidateBot();
            _logger.LogInformation(
                "Successfully got Twitch API bot access token info! Client-ID: {ClientID}, User-ID: {UserID}, " +
                "Login: {Login}, Expires in: {Expires}s, Scopes: {Scopes}", botTokenInfo.ClientId,
                botTokenInfo.UserId, botTokenInfo.Login, botTokenInfo.ExpiresIn, botTokenInfo.Scopes);
            ValidateAccessTokenResponse channelTokenInfo = await TwitchApi.ValidateChannel();
            _logger.LogInformation(
                "Successfully got Twitch API channel access token info! Client-ID: {ClientID}, User-ID: {UserID}, " +
                "Login: {Login}, Expires in: {Expires}s, Scopes: {Scopes}", channelTokenInfo.ClientId,
                channelTokenInfo.UserId, channelTokenInfo.Login, channelTokenInfo.ExpiresIn, channelTokenInfo.Scopes);

            // Validate correct usernames
            if (!botTokenInfo.Login.Equals(_botUsername, StringComparison.InvariantCultureIgnoreCase))
                _logger.LogWarning("Bot token login '{Login}' does not match configured bot username '{Username}'",
                    botTokenInfo.Login, _botUsername);
            if (!channelTokenInfo.Login.Equals(_channelName, StringComparison.InvariantCultureIgnoreCase))
                _logger.LogWarning("Channel token login '{Login}' does not match configured channel '{Channel}'",
                    botTokenInfo.Login, _channelName);

            // Validate correct Client-IDs
            if (!botTokenInfo.ClientId.Equals(_appClientId, StringComparison.InvariantCultureIgnoreCase))
                _logger.LogWarning(
                    "Bot token Client-ID '{ClientID}' does not match configured App-Client-ID '{AppClientId}'. " +
                    "Did you create the token using the wrong App-Client-ID?", botTokenInfo.ClientId, _appClientId);
            if (!channelTokenInfo.ClientId.Equals(_appClientId, StringComparison.InvariantCultureIgnoreCase))
                _logger.LogWarning(
                    "Channel token Client-ID '{ClientID}' does not match configured App-Client-ID '{AppClientId}'. " +
                    "Did you create the token using the wrong App-Client-ID?", channelTokenInfo.ClientId, _appClientId);

            // Validate Scopes
            foreach ((string scope, ScopeInfo scopeInfo) in ScopeInfosPerScope)
            {
                if (scopeInfo.ScopeType == ScopeType.Bot && !botTokenInfo.Scopes.ToHashSet().Contains(scope))
                    _logger.LogWarning("Missing Twitch-API scope '{Scope}' (bot), needed for: {NeededFor}",
                        scope, scopeInfo.NeededFor);
                if (scopeInfo.ScopeType == ScopeType.Channel && !channelTokenInfo.Scopes.ToHashSet().Contains(scope))
                    _logger.LogWarning("Missing Twitch-API scope '{Scope}' (channel), needed for: {NeededFor}",
                        scope, scopeInfo.NeededFor);
            }
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            await ValidateApiTokens();

            List<Task> tasks = [];
            tasks.Add(TwitchEventSubChat.Start(cancellationToken));
            await TaskUtils.WhenAllFastExit(tasks);

            await _twitchChatSender.DisposeAsync();
            TwitchEventSubChat.IncomingMessage -= MessageReceived;
            _logger.LogDebug("twitch chat is now fully shut down");
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
