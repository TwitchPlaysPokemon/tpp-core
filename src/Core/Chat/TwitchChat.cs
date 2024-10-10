using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using Core.Configuration;
using Core.Moderation;
using Core.Overlay;
using Core.Utils;
using Persistence;
using User = Model.User;

namespace Core.Chat
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
        private readonly TwitchChatSender _twitchChatSender;
        private readonly TwitchChatModeChanger _twitchChatModeChanger;
        private readonly TwitchChatExecutor _twitchChatExecutor;
        public EventSubChat EventSubChat { get; }

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
            EventSubChat = new EventSubChat(loggerFactory, clock, TwitchApi, userRepo,
                subscriptionProcessor, overlayConnection, _twitchChatSender,
                chatConfig.ChannelId, chatConfig.UserId,
                chatConfig.CoStreamInputsEnabled, chatConfig.CoStreamInputsOnlyLive, coStreamChannelsRepo);

            EventSubChat.IncomingMessage += MessageReceived;
            _twitchChatModeChanger = new TwitchChatModeChanger(
                loggerFactory.CreateLogger<TwitchChatModeChanger>(), TwitchApi, chatConfig);
            _twitchChatExecutor = new TwitchChatExecutor(loggerFactory.CreateLogger<TwitchChatExecutor>(),
                TwitchApi, chatConfig);
        }

        private void MessageReceived(object? sender, MessageEventArgs args)
        {
            IncomingMessage?.Invoke(this, args);
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            foreach (string problem in await TwitchApi.DetectProblems(_botUsername, _channelName))
                _logger.LogWarning("TwitchAPI problem detected: {Problem}", problem);

            List<Task> tasks = [];
            tasks.Add(EventSubChat.Start(cancellationToken));
            await TaskUtils.WhenAllFastExit(tasks);

            await _twitchChatSender.DisposeAsync();
            EventSubChat.IncomingMessage -= MessageReceived;
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
