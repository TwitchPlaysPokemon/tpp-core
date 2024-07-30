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

            TwitchApi = new TwitchApi(
                loggerFactory,
                clock,
                chatConfig.InfiniteAccessToken,
                chatConfig.RefreshToken,
                chatConfig.AppClientId,
                chatConfig.AppClientSecret);
            TwitchEventSubChat = new TwitchEventSubChat(loggerFactory, clock, TwitchApi, userRepo,
                chatConfig.ChannelId, chatConfig.UserId,
                chatConfig.CoStreamInputsEnabled, chatConfig.CoStreamInputsOnlyLive, coStreamChannelsRepo);

            TwitchEventSubChat.IncomingMessage += MessageReceived;
            _twitchChatSender = new TwitchChatSender(loggerFactory, TwitchApi, chatConfig, useTwitchReplies);
            _twitchChatModeChanger = new TwitchChatModeChanger(
                loggerFactory.CreateLogger<TwitchChatModeChanger>(), TwitchApi, chatConfig);
            _twitchChatExecutor = new TwitchChatExecutor(loggerFactory.CreateLogger<TwitchChatExecutor>(),
                TwitchApi, chatConfig);
        }

        private void MessageReceived(object? sender, MessageEventArgs args)
        {
            IncomingMessage?.Invoke(this, args);
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
