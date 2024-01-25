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
using TPP.Core.Overlay;
using TPP.Core.Overlay.Events;
using TPP.Model;
using TPP.Persistence;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using static TPP.Core.Configuration.ConnectionConfig.Twitch;
using static TPP.Core.EventUtils;
using OnConnectedEventArgs = TwitchLib.Client.Events.OnConnectedEventArgs;

namespace TPP.Core.Chat
{
    public sealed class TwitchChat : IChat
    {
        public string Name { get; }
        public event EventHandler<MessageEventArgs>? IncomingMessage;

        /// Twitch Messaging Interface (TMI, the somewhat IRC-compatible protocol twitch uses) maximum message length.
        /// This limit is in characters, not bytes. See https://discuss.dev.twitch.tv/t/message-character-limit/7793/6
        private const int MaxMessageLength = 500;
        /// Maximum message length for whispers if the target user hasn't whispered us before.
        /// See also https://dev.twitch.tv/docs/api/reference/#send-whisper
        private const int MaxWhisperLength = 500;
        /// Maximum message length for whispers if the target user _has_ whispered us before.
        /// See also https://dev.twitch.tv/docs/api/reference/#send-whisper
        /// TODO Either Twitch's API is broken or their documentation is wrong,
        ///      because even for users that have whispered us before they just truncate the message to 500 characters.
        ///      See also https://discuss.dev.twitch.tv/t/whisper-truncated-to-500-characters-even-for-users-that-have-whispered-us-before/44844?u=felk
        // private const int MaxRepeatedWhisperLength = 10000;
        private const int MaxRepeatedWhisperLength = 500;

        private static readonly MessageSplitter MessageSplitterRegular = new(
            maxMessageLength: MaxMessageLength - "/me ".Length);

        private static readonly MessageSplitter MessageSplitterWhisperNeverWhispered = new(
            maxMessageLength: MaxWhisperLength);

        private static readonly MessageSplitter MessageSplitterWhisperWereWhisperedBefore = new(
            maxMessageLength: MaxRepeatedWhisperLength);

        private readonly ILogger<TwitchChat> _logger;
        private readonly IClock _clock;
        private readonly string _channel;
        public readonly string ChannelId;
        private readonly string _userId;
        private readonly ImmutableHashSet<SuppressionType> _suppressions;
        private readonly ImmutableHashSet<string> _suppressionOverrides;
        private readonly IUserRepo _userRepo;
        private readonly IChattersSnapshotsRepo _chattersSnapshotsRepo;
        private readonly ISubscriptionProcessor _subscriptionProcessor;
        private readonly TwitchClient _twitchClient;
        private readonly TwitchApiProvider _twitchApiProvider;
        private readonly TwitchLibSubscriptionWatcher _subscriptionWatcher;
        private readonly OverlayConnection _overlayConnection;
        private readonly TwitchChatQueue _queue;
        private readonly Duration _getChattersInterval;

        private readonly bool _useTwitchReplies;

        private bool _connected = false;
        private Action? _workersCleanup;

        public TwitchChat(
            string name,
            ILoggerFactory loggerFactory,
            IClock clock,
            ConnectionConfig.Twitch chatConfig,
            IUserRepo userRepo,
            IChattersSnapshotsRepo chattersSnapshotsRepo,
            ISubscriptionProcessor subscriptionProcessor,
            OverlayConnection overlayConnection,
            bool useTwitchReplies = true)
        {
            Name = name;
            _logger = loggerFactory.CreateLogger<TwitchChat>();
            _clock = clock;
            _channel = chatConfig.Channel;
            ChannelId = chatConfig.ChannelId;
            _userId = chatConfig.UserId;
            _suppressions = chatConfig.Suppressions;
            _suppressionOverrides = chatConfig.SuppressionOverrides
                .Select(s => s.ToLowerInvariant()).ToImmutableHashSet();
            _userRepo = userRepo;
            _chattersSnapshotsRepo = chattersSnapshotsRepo;
            _subscriptionProcessor = subscriptionProcessor;
            _overlayConnection = overlayConnection;
            _useTwitchReplies = useTwitchReplies;
            _getChattersInterval = chatConfig.GetChattersInterval;

            _twitchApiProvider = new TwitchApiProvider(
                loggerFactory,
                clock,
                chatConfig.AccessToken,
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

            _queue = new TwitchChatQueue(
                loggerFactory.CreateLogger<TwitchChatQueue>(),
                chatConfig.UserId,
                _twitchApiProvider,
                _twitchClient);
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
                await SendWhisper(e.Subscriber, response);

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

                await _overlayConnection.Send(new NewSubscriber
                {
                    User = e.SubscriptionInfo.Subscriber,
                    Emotes = e.SubscriptionInfo.Emotes.Select(EmoteInfo.FromOccurence).ToImmutableList(),
                    SubMessage = e.SubscriptionInfo.Message,
                    ShareSub = false,
                }, CancellationToken.None);
            });
        }

        public async Task SendMessage(string message, Message? responseTo = null)
        {
            if (_suppressions.Contains(SuppressionType.Message) &&
                !_suppressionOverrides.Contains(_channel))
            {
                _logger.LogDebug("(suppressed) >#{Channel}: {Message}", _channel, message);
                return;
            }
            _logger.LogDebug(">#{Channel}: {Message}", _channel, message);
            await Task.Run(() =>
            {
                if (responseTo != null && !_useTwitchReplies)
                    message = $"@{responseTo.User.Name} " + message;
                foreach (string part in MessageSplitterRegular.FitToMaxLength(message))
                {
                    if (_useTwitchReplies && responseTo?.Details.MessageId != null)
                        _queue.Enqueue(responseTo.User,
                            new OutgoingMessage.Reply(_channel, Message: "/me " + part,
                                ReplyToId: responseTo.Details.MessageId));
                    else
                        _queue.Enqueue(responseTo?.User, new OutgoingMessage.Chat(_channel, "/me " + part));
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
            bool newRecipient = target.LastWhisperReceivedAt == null;
            MessageSplitter splitter = newRecipient
                ? MessageSplitterWhisperNeverWhispered
                : MessageSplitterWhisperWereWhisperedBefore;
            await Task.Run(() =>
            {
                foreach (string part in splitter.FitToMaxLength(message))
                {
                    _queue.Enqueue(target, new OutgoingMessage.Whisper(target.Id, part, newRecipient));
                }
            });
        }

        public async Task Connect()
        {
            if (_connected)
            {
                throw new InvalidOperationException("Can only ever connect once per chat instance.");
            }
            _connected = true;
            await _twitchClient.ConnectAsync();
            var tokenSource = new CancellationTokenSource();
            Task sendWorker = _queue.StartSendWorker(tokenSource.Token);
            Task checkConnectivityWorker = CheckConnectivityWorker(tokenSource.Token);
            Task chattersWorker = ChattersWorker(tokenSource.Token);
            _workersCleanup = () =>
            {
                tokenSource.Cancel();
                if (!sendWorker.IsCanceled) sendWorker.Wait();
                if (!checkConnectivityWorker.IsCanceled) checkConnectivityWorker.Wait();
                if (!chattersWorker.IsCanceled) chattersWorker.Wait();
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
                        await _twitchClient.ReconnectAsync();
                        _logger.LogInformation("Successfully reconnected to twitch.");
                    }
                    catch (Exception)
                    {
                        _logger.LogError("Failed to reconnect, trying again in {Delay} seconds", delay.TotalSeconds);
                    }
                }

                await Task.Delay(delay, cancellationToken);
            }
        }

        private async Task<List<Chatter>> GetChatters()
        {
            List<Chatter> chatters = new();
            string? nextCursor = null;
            do
            {
                GetChattersResponse getChattersResponse = await (await _twitchApiProvider.Get()).Helix
                    .Chat.GetChattersAsync(ChannelId, _userId, first: 1000, after: nextCursor);
                chatters.AddRange(getChattersResponse.Data);
                nextCursor = getChattersResponse.Pagination?.Cursor;
            } while (nextCursor != null);
            return chatters;
        }

        private async Task ChattersWorker(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    List<Chatter> chatters = await GetChatters();

                    ImmutableList<string> chatterNames = chatters.Select(c => c.UserLogin).ToImmutableList();
                    ImmutableList<string> chatterIds = chatters.Select(c => c.UserId).ToImmutableList();
                    await _chattersSnapshotsRepo.LogChattersSnapshot(
                        chatterNames, chatterIds, _channel, _clock.GetCurrentInstant());
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed retrieving chatters list");
                }

                await Task.Delay(_getChattersInterval.ToTimeSpan(), cancellationToken);
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

        public void Dispose()
        {
            if (_connected)
            {
                _workersCleanup?.Invoke();
                _twitchClient.DisconnectAsync();
            }
            _twitchClient.OnConnected -= Connected;
            _subscriptionWatcher.Subscribed -= OnSubscribed;
            _subscriptionWatcher.SubscriptionGifted -= OnSubscriptionGifted;
            _subscriptionWatcher.Dispose();
            _twitchClient.OnMessageReceived -= MessageReceived;
            _twitchClient.OnWhisperReceived -= WhisperReceived;
            _logger.LogDebug("twitch chat is now fully shut down");
        }

        private async Task<ChatSettings> GetChatSettings()
        {
            TwitchAPI twitchApi = await _twitchApiProvider.Get();
            GetChatSettingsResponse settingsResponse =
                await twitchApi.Helix.Chat.GetChatSettingsAsync(ChannelId, _userId);
            // From the Twitch API documentation https://dev.twitch.tv/docs/api/reference/#update-chat-settings
            //   'data': The list of chat settings. The list contains a single object with all the settings
            ChatSettingsResponseModel settings = settingsResponse.Data[0];
            return new ChatSettings
            {
                EmoteMode = settings.EmoteMode,
                FollowerMode = settings.FollowerMode,
                FollowerModeDuration = settings.FollowerModeDuration,
                SlowMode = settings.SlowMode,
                SlowModeWaitTime = settings.SlowModeWaitDuration,
                SubscriberMode = settings.SubscriberMode,
                UniqueChatMode = settings.UniqueChatMode,
                NonModeratorChatDelay = settings.NonModeratorChatDelay,
                NonModeratorChatDelayDuration = settings.NonModeratorChatDelayDuration,
            };
        }

        public async Task EnableEmoteOnly()
        {
            if (_suppressions.Contains(SuppressionType.Command) &&
                !_suppressionOverrides.Contains(_channel))
            {
                _logger.LogDebug($"(suppressed) enabling emote only mode in #{_channel}");
                return;
            }

            _logger.LogDebug($"enabling emote only mode in #{_channel}");
            TwitchAPI twitchApi = await _twitchApiProvider.Get();
            ChatSettings chatSettings = await GetChatSettings();
            chatSettings.EmoteMode = true;
            await twitchApi.Helix.Chat.UpdateChatSettingsAsync(ChannelId, _userId, chatSettings);
        }

        public async Task DisableEmoteOnly()
        {
            if (_suppressions.Contains(SuppressionType.Command) &&
                !_suppressionOverrides.Contains(_channel))
            {
                _logger.LogDebug($"(suppressed) disabling emote only mode in #{_channel}");
                return;
            }

            _logger.LogDebug($"disabling emote only mode in #{_channel}");
            TwitchAPI twitchApi = await _twitchApiProvider.Get();
            ChatSettings chatSettings = await GetChatSettings();
            chatSettings.EmoteMode = false;
            await twitchApi.Helix.Chat.UpdateChatSettingsAsync(ChannelId, _userId, chatSettings);
        }

        public async Task DeleteMessage(string messageId)
        {
            if (_suppressions.Contains(SuppressionType.Command) &&
                !_suppressionOverrides.Contains(_channel))
            {
                _logger.LogDebug($"(suppressed) deleting message {messageId} in #{_channel}");
                return;
            }

            _logger.LogDebug($"deleting message {messageId} in #{_channel}");
            TwitchAPI twitchApi = await _twitchApiProvider.Get();
            await twitchApi.Helix.Moderation.DeleteChatMessagesAsync(ChannelId, _userId, messageId);
        }

        public async Task Timeout(User user, string? message, Duration duration)
        {
            if (_suppressions.Contains(SuppressionType.Command) &&
                !(_suppressionOverrides.Contains(_channel) && _suppressionOverrides.Contains(user.SimpleName)))
            {
                _logger.LogDebug($"(suppressed) time out {user} for {duration} in #{_channel}: {message}");
                return;
            }

            _logger.LogDebug($"time out {user} for {duration} in #{_channel}: {message}");
            TwitchAPI twitchApi = await _twitchApiProvider.Get();
            var banUserRequest = new BanUserRequest
            {
                UserId = user.Id,
                Duration = (int)duration.TotalSeconds,
                Reason = message ?? "no timeout reason was given",
            };
            await twitchApi.Helix.Moderation.BanUserAsync(ChannelId, _userId, banUserRequest);
        }

        public async Task Ban(User user, string? message)
        {
            if (_suppressions.Contains(SuppressionType.Command) &&
                !(_suppressionOverrides.Contains(_channel) && _suppressionOverrides.Contains(user.SimpleName)))
            {
                _logger.LogDebug($"(suppressed) ban {user} in #{_channel}: {message}");
                return;
            }

            _logger.LogDebug($"ban {user} in #{_channel}: {message}");
            TwitchAPI twitchApi = await _twitchApiProvider.Get();
            var banUserRequest = new BanUserRequest
            {
                UserId = user.Id,
                Duration = null,
                Reason = message ?? "no ban reason was given",
            };
            await twitchApi.Helix.Moderation.BanUserAsync(ChannelId, _userId, banUserRequest);
        }

        public async Task Unban(User user, string? message)
        {
            if (_suppressions.Contains(SuppressionType.Command) &&
                !(_suppressionOverrides.Contains(_channel) && _suppressionOverrides.Contains(user.SimpleName)))
            {
                _logger.LogDebug($"(suppressed) unban {user} in #{_channel}: {message}");
                return;
            }

            _logger.LogDebug($"unban {user} in #{_channel}: {message}");
            TwitchAPI twitchApi = await _twitchApiProvider.Get();
            await twitchApi.Helix.Moderation.UnbanUserAsync(ChannelId, _userId, user.Id);
        }

        public async Task<TwitchAPI> GetTwitchApi() => await _twitchApiProvider.Get();
    }
}
