using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Common;
using TPP.Core.Configuration;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using static TPP.Core.Configuration.ConnectionConfig.Twitch;

namespace TPP.Core.Chat
{
    public sealed class TwitchChat : IChat
    {
        public string Name { get; }
        public event EventHandler<MessageEventArgs> IncomingMessage = null!;

        /// Twitch Messaging Interface (TMI, the somewhat IRC-compatible protocol twitch uses) maximum message length.
        /// This limit is in characters, not bytes. See https://discuss.dev.twitch.tv/t/message-character-limit/7793/6
        private const int MaxMessageLength = 500;

        private static readonly MessageSplitter MessageSplitterRegular = new(
            maxMessageLength: MaxMessageLength - "/me ".Length);

        private static readonly MessageSplitter MessageSplitterWhisper = new(
            // visual representation of the longest possible username (25 characters)
            maxMessageLength: MaxMessageLength - "/w ,,,,,''''',,,,,''''',,,,, ".Length);

        private readonly ILogger<TwitchChat> _logger;
        private readonly IClock _clock;
        private readonly string _ircChannel;
        private readonly ImmutableHashSet<SuppressionType> _suppressions;
        private readonly ImmutableHashSet<string> _suppressionOverrides;
        private readonly IUserRepo _userRepo;
        private readonly ISubscriptionProcessor _subscriptionProcessor;
        private readonly TwitchClient _twitchClient;
        private readonly TwitchLibSubscriptionWatcher _subscriptionWatcher;

        private bool _connected = false;
        private Action? _connectivityWorkerCleanup;

        public TwitchChat(
            string name,
            ILoggerFactory loggerFactory,
            IClock clock,
            ConnectionConfig.Twitch chatConfig,
            IUserRepo userRepo,
            ISubscriptionProcessor subscriptionProcessor)
        {
            Name = name;
            _logger = loggerFactory.CreateLogger<TwitchChat>();
            _clock = clock;
            _ircChannel = chatConfig.Channel;
            _suppressions = chatConfig.Suppressions;
            _suppressionOverrides = chatConfig.SuppressionOverrides
                .Select(s => s.ToLowerInvariant()).ToImmutableHashSet();
            _userRepo = userRepo;
            _subscriptionProcessor = subscriptionProcessor;

            _twitchClient = new TwitchClient(
                client: new WebSocketClient(new ClientOptions()),
                logger: loggerFactory.CreateLogger<TwitchClient>());
            var credentials = new ConnectionCredentials(
                twitchUsername: chatConfig.Username,
                twitchOAuth: chatConfig.Password,
                disableUsernameCheck: true);
            _twitchClient.Initialize(
                credentials: credentials,
                channel: chatConfig.Channel,
                // disable TwitchLib's command features, we do that ourselves
                chatCommandIdentifier: '\0',
                whisperCommandIdentifier: '\0');

            _twitchClient.OnConnected += Connected;
            _twitchClient.OnMessageReceived += MessageReceived;
            _twitchClient.OnWhisperReceived += WhisperReceived;
            _subscriptionWatcher = new TwitchLibSubscriptionWatcher(_userRepo, _twitchClient, clock);
            _subscriptionWatcher.Subscribed += OnSubscribed;
            _subscriptionWatcher.SubscriptionGifted += OnSubscriptionGifted;
        }

        private void Connected(object? sender, OnConnectedArgs e) => _twitchClient.JoinChannel(_ircChannel);

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

        private async void OnSubscribed(object? sender, SubscriptionInfo e)
        {
            ISubscriptionProcessor.SubResult subResult = await _subscriptionProcessor.ProcessSubscription(e);
            string response = BuildSubResponse(subResult, null, false);
            await SendWhisper(e.Subscriber, response);

            // TODO send to overlay
        }

        private async void OnSubscriptionGifted(object? sender, SubscriptionGiftInfo e)
        {
            (ISubscriptionProcessor.SubResult subResult, ISubscriptionProcessor.SubGiftResult subGiftResult) =
                await _subscriptionProcessor.ProcessSubscriptionGift(e);

            string subResponse = BuildSubResponse(subResult, e.Gifter, e.IsAnonymous);
            await SendWhisper(e.SubscriptionInfo.Subscriber, subResponse);

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
                await SendWhisper(e.Gifter, subGiftResponse); // don't respond to the "AnAnonymousGifter" user

            // TODO send to overlay
        }

        public async Task SendMessage(string message)
        {
            if (_suppressions.Contains(SuppressionType.Message) &&
                !_suppressionOverrides.Contains(_ircChannel))
            {
                _logger.LogDebug("(suppressed) >#{Channel}: {Message}", _ircChannel, message);
                return;
            }
            _logger.LogDebug(">#{Channel}: {Message}", _ircChannel, message);
            await Task.Run(() =>
            {
                foreach (string part in MessageSplitterRegular.FitToMaxLength(message))
                {
                    _twitchClient.SendMessage(_ircChannel, "/me " + part);
                }
            });
        }

        public async Task SendWhisper(User target, string message)
        {
            if (_suppressions.Contains(SuppressionType.Whisper) &&
                !_suppressionOverrides.Contains(target.SimpleName))
            {
                _logger.LogDebug("(suppressed) >@{Username}: {Message}", target.SimpleName, message);
                return;
            }
            _logger.LogDebug(">@{Username}: {Message}", target.SimpleName, message);
            await Task.Run(() =>
            {
                foreach (string part in MessageSplitterWhisper.FitToMaxLength(message))
                {
                    _twitchClient.SendWhisper(target.SimpleName, part);
                }
            });
        }

        public void Connect()
        {
            if (_connected)
            {
                throw new InvalidOperationException("Can only ever connect once per chat instance.");
            }
            _connected = true;
            _twitchClient.Connect();
            var tokenSource = new CancellationTokenSource();
            Task checkConnectivityWorker = CheckConnectivityWorker(tokenSource.Token);
            _connectivityWorkerCleanup = () =>
            {
                tokenSource.Cancel();
                if (!checkConnectivityWorker.IsCanceled) checkConnectivityWorker.Wait();
            };
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
                        _twitchClient.Reconnect();
                    }
                    catch (Exception)
                    {
                        _logger.LogError("Failed to reconnect, trying again in {Delay} seconds", delay.TotalSeconds);
                    }
                }

                await Task.Delay(delay, cancellationToken);
            }
        }

        private async void MessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            _logger.LogDebug("<#{Channel} {Username}: {Message}",
                _ircChannel, e.ChatMessage.Username, e.ChatMessage.Message);
            await AnyMessageReceived(e.ChatMessage, e.ChatMessage.Message, MessageSource.Chat);
        }

        private async void WhisperReceived(object? sender, OnWhisperReceivedArgs e)
        {
            _logger.LogDebug("<@{Username}: {Message}", e.WhisperMessage.Username, e.WhisperMessage.Message);
            await AnyMessageReceived(e.WhisperMessage, e.WhisperMessage.Message, MessageSource.Whisper);
        }

        private async Task AnyMessageReceived(
            TwitchLibMessage twitchLibMessage,
            string messageText,
            MessageSource source)
        {
            string? colorHex = twitchLibMessage.ColorHex;
            User user = await _userRepo.RecordUser(new UserInfo(
                id: twitchLibMessage.UserId,
                twitchDisplayName: twitchLibMessage.DisplayName,
                simpleName: twitchLibMessage.Username,
                color: string.IsNullOrEmpty(colorHex) ? null : HexColor.FromWithHash(colorHex),
                fromMessage: true,
                updatedAt: _clock.GetCurrentInstant()
            ));
            Message message = new(user, messageText, source, twitchLibMessage.RawIrcMessage);
            IncomingMessage?.Invoke(this, new MessageEventArgs(message));
        }

        public void Dispose()
        {
            if (_connected)
            {
                _connectivityWorkerCleanup?.Invoke();
                _twitchClient.Disconnect();
            }
            _twitchClient.OnConnected -= Connected;
            _subscriptionWatcher.Subscribed -= OnSubscribed;
            _subscriptionWatcher.SubscriptionGifted -= OnSubscriptionGifted;
            _subscriptionWatcher.Dispose();
            _twitchClient.OnMessageReceived -= MessageReceived;
            _twitchClient.OnWhisperReceived -= WhisperReceived;
            _logger.LogDebug("twitch chat is now fully shut down");
        }

        public async Task EnableEmoteOnly()
        {
            if (_suppressions.Contains(SuppressionType.Command) &&
                !_suppressionOverrides.Contains(_ircChannel))
            {
                _logger.LogDebug($"(suppressed) enabling emote only mode in #{_ircChannel}");
                return;
            }

            _logger.LogDebug($"enabling emote only mode in #{_ircChannel}");
            await Task.Run(() => _twitchClient.EmoteOnlyOn(_ircChannel));
        }

        public async Task DisableEmoteOnly()
        {
            if (_suppressions.Contains(SuppressionType.Command) &&
                !_suppressionOverrides.Contains(_ircChannel))
            {
                _logger.LogDebug($"(suppressed) disabling emote only mode in #{_ircChannel}");
                return;
            }

            _logger.LogDebug($"disabling emote only mode in #{_ircChannel}");
            await Task.Run(() => _twitchClient.EmoteOnlyOff(_ircChannel));
        }
    }
}
