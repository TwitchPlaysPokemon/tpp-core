using System;
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
using TPP.Core.Overlay.Events;
using TPP.Model;
using TPP.Persistence;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using static TPP.Core.EventUtils;
using OnConnectedEventArgs = TwitchLib.Client.Events.OnConnectedEventArgs;

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
        private readonly IUserRepo _userRepo;
        private readonly ISubscriptionProcessor _subscriptionProcessor;
        private readonly TwitchClient _twitchClient;
        public readonly TwitchApiProvider TwitchApiProvider;
        private readonly TwitchLibSubscriptionWatcher _subscriptionWatcher;
        private readonly OverlayConnection _overlayConnection;
        private readonly TwitchChatSender _twitchChatSender;
        private readonly TwitchChatModeChanger _twitchChatModeChanger;
        private readonly TwitchChatExecutor _twitchChatExecutor;

        public TwitchChat(
            string name,
            ILoggerFactory loggerFactory,
            IClock clock,
            ConnectionConfig.Twitch chatConfig,
            IUserRepo userRepo,
            ISubscriptionProcessor subscriptionProcessor,
            OverlayConnection overlayConnection,
            bool useTwitchReplies = true)
        {
            Name = name;
            _logger = loggerFactory.CreateLogger<TwitchChat>();
            _clock = clock;
            _channel = chatConfig.Channel;
            ChannelId = chatConfig.ChannelId;
            _userRepo = userRepo;
            _subscriptionProcessor = subscriptionProcessor;
            _overlayConnection = overlayConnection;

            TwitchApiProvider = new TwitchApiProvider(
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
            _subscriptionWatcher = new TwitchLibSubscriptionWatcher(
                loggerFactory.CreateLogger<TwitchLibSubscriptionWatcher>(), _userRepo, _twitchClient, clock);
            _subscriptionWatcher.Subscribed += OnSubscribed;
            _subscriptionWatcher.SubscriptionGifted += OnSubscriptionGifted;

            _twitchChatSender = new TwitchChatSender(loggerFactory, TwitchApiProvider, chatConfig, useTwitchReplies);
            _twitchChatModeChanger = new TwitchChatModeChanger(
                loggerFactory.CreateLogger<TwitchChatModeChanger>(), TwitchApiProvider, chatConfig);
            _twitchChatExecutor = new TwitchChatExecutor(loggerFactory.CreateLogger<TwitchChatExecutor>(),
                TwitchApiProvider, chatConfig);
        }

        private Task Connected(object? sender, OnConnectedEventArgs e) => _twitchClient.JoinChannelAsync(_channel);

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

        private static string BuildSubResponse(
            ISubscriptionProcessor.SubResult subResult, User? gifter, bool isAnonymous)
        {
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

            return subResult switch
            {
                ISubscriptionProcessor.SubResult.Ok ok => BuildOkMessage(ok, gifter, isAnonymous),
                ISubscriptionProcessor.SubResult.SameMonth sameMonth =>
                    $"We detected that you've already announced your resub for month {sameMonth.Month}, " +
                    "and received the appropriate tokens. " +
                    "If you believe this is in error, please contact a moderator so this can be corrected.",
                _ => throw new ArgumentOutOfRangeException(nameof(subResult)),
            };
        }

        private void OnSubscribed(object? sender, SubscriptionInfo e)
        {
            TaskToVoidSafely(_logger, async () =>
            {
                ISubscriptionProcessor.SubResult subResult = await _subscriptionProcessor.ProcessSubscription(e);
                string response = BuildSubResponse(subResult, null, false);
                await _twitchChatSender.SendWhisper(e.Subscriber, response);

                await _overlayConnection.Send(new NewSubscriber
                {
                    User = e.Subscriber,
                    Emotes = e.Emotes.Select(EmoteInfo.FromOccurence).ToImmutableList(),
                    SubMessage = e.Message,
                    ShareSub = true,
                }, CancellationToken.None);
            });
        }

        private void OnSubscriptionGifted(object? sender, SubscriptionGiftInfo e)
        {
            TaskToVoidSafely(_logger, async () =>
            {
                (ISubscriptionProcessor.SubResult subResult, ISubscriptionProcessor.SubGiftResult subGiftResult) =
                    await _subscriptionProcessor.ProcessSubscriptionGift(e);

                string subResponse = BuildSubResponse(subResult, e.Gifter, e.IsAnonymous);
                await _twitchChatSender.SendWhisper(e.SubscriptionInfo.Subscriber, subResponse);

                string subGiftResponse = subGiftResult switch
                {
                    ISubscriptionProcessor.SubGiftResult.LinkedAccount =>
                        $"As you are linked to the account '{e.SubscriptionInfo.Subscriber.Name}' you have gifted to, " +
                        "you have not received a token bonus. " +
                        "The recipient account still gains the normal benefits however. Thanks for subscribing!",
                    ISubscriptionProcessor.SubGiftResult.SameMonth { Month: var month } =>
                        $"We detected that this gift sub may have been a repeated message for month {month}, " +
                        "and you have already received the appropriate tokens. " +
                        "If you believe this is in error, please contact a moderator so this can be corrected.",
                    ISubscriptionProcessor.SubGiftResult.Ok { GifterTokens: var tokens } =>
                        $"Thank you for your generosity! You received T{tokens} tokens for giving a gift " +
                        "subscription. The recipient has been notified and awarded their token benefits.",
                    _ => throw new ArgumentOutOfRangeException(nameof(subGiftResult))
                };
                if (!e.IsAnonymous)
                    await _twitchChatSender.SendWhisper(e.Gifter,
                        subGiftResponse); // don't respond to the "AnAnonymousGifter" user

                await _overlayConnection.Send(new NewSubscriber
                {
                    User = e.SubscriptionInfo.Subscriber,
                    Emotes = e.SubscriptionInfo.Emotes.Select(EmoteInfo.FromOccurence).ToImmutableList(),
                    SubMessage = e.SubscriptionInfo.Message,
                    ShareSub = false,
                }, CancellationToken.None);
            });
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            await _twitchClient.ConnectAsync();

            // Must wait on all concurrently running tasks simultaneously to know when one of them crashed
            await Task.WhenAll(
                CheckConnectivityWorker(cancellationToken)
            );

            await _twitchClient.DisconnectAsync();
            await _twitchChatSender.DisposeAsync();
            _twitchClient.OnConnected -= Connected;
            _subscriptionWatcher.Subscribed -= OnSubscribed;
            _subscriptionWatcher.SubscriptionGifted -= OnSubscriptionGifted;
            _subscriptionWatcher.Dispose();
            _twitchClient.OnMessageReceived -= MessageReceived;
            _twitchClient.OnWhisperReceived -= WhisperReceived;
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

        private async Task MessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            _logger.LogDebug("<#{Channel} {Username}: {Message}",
                _channel, e.ChatMessage.Username, e.ChatMessage.Message);
            if (e.ChatMessage.Username == _twitchClient.TwitchUsername)
                // new core sees messages posted by old core, but we don't want to process our own messages
                return;
            User user = await _userRepo.RecordUser(GetUserInfoFromTwitchMessage(e.ChatMessage, fromWhisper: false));
            var message = new Message(user, e.ChatMessage.Message, MessageSource.Chat, e.ChatMessage.RawIrcMessage)
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

        private async Task WhisperReceived(object? sender, OnWhisperReceivedArgs e)
        {
            _logger.LogDebug("<@{Username}: {Message}", e.WhisperMessage.Username, e.WhisperMessage.Message);
            User user = await _userRepo.RecordUser(
                GetUserInfoFromTwitchMessage(e.WhisperMessage, fromWhisper: true));
            var message = new Message(user, e.WhisperMessage.Message, MessageSource.Whisper,
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
