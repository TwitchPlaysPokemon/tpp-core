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
        private readonly TwitchClient _twitchClient;

        private bool _connected = false;
        private Action? _connectivityWorkerCleanup;

        public TwitchChat(
            string name,
            ILoggerFactory loggerFactory,
            IClock clock,
            ConnectionConfig.Twitch chatConfig,
            IUserRepo userRepo)
        {
            Name = name;
            _logger = loggerFactory.CreateLogger<TwitchChat>();
            _clock = clock;
            _ircChannel = chatConfig.Channel;
            _suppressions = chatConfig.Suppressions;
            _suppressionOverrides = chatConfig.SuppressionOverrides
                .Select(s => s.ToLowerInvariant()).ToImmutableHashSet();
            _userRepo = userRepo;

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
        }

        private void Connected(object? sender, OnConnectedArgs e) => _twitchClient.JoinChannel(_ircChannel);

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
            User user = await _userRepo.RecordUser(GetUserInfoFromTwitchMessage(e.ChatMessage));
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

        private async void WhisperReceived(object? sender, OnWhisperReceivedArgs e)
        {
            _logger.LogDebug("<@{Username}: {Message}", e.WhisperMessage.Username, e.WhisperMessage.Message);
            User user = await _userRepo.RecordUser(GetUserInfoFromTwitchMessage(e.WhisperMessage));
            var message = new Message(user, e.WhisperMessage.Message, MessageSource.Whisper, e.WhisperMessage.RawIrcMessage)
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

        private UserInfo GetUserInfoFromTwitchMessage(TwitchLibMessage message)
        {
            string? colorHex = message.ColorHex;
            return new UserInfo(
                id: message.UserId,
                twitchDisplayName: message.DisplayName,
                simpleName: message.Username,
                color: string.IsNullOrEmpty(colorHex) ? null : HexColor.FromWithHash(colorHex),
                fromMessage: true,
                updatedAt: _clock.GetCurrentInstant()
            );
        }

        public void Dispose()
        {
            if (_connected)
            {
                _connectivityWorkerCleanup?.Invoke();
                _twitchClient.Disconnect();
            }
            _twitchClient.OnConnected -= Connected;
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

        public async Task DeleteMessage(string messageId)
        {
            if (_suppressions.Contains(SuppressionType.Command) &&
                !_suppressionOverrides.Contains(_ircChannel))
            {
                _logger.LogDebug($"(suppressed) deleting message {messageId} in #{_ircChannel}");
                return;
            }

            _logger.LogDebug($"deleting message {messageId} in #{_ircChannel}");
            await Task.Run(() => _twitchClient.SendMessage(_ircChannel, ".delete " + messageId));
        }

        public async Task Timeout(User user, string? message, Duration duration)
        {
            if (_suppressions.Contains(SuppressionType.Command) &&
                !(_suppressionOverrides.Contains(_ircChannel) && _suppressionOverrides.Contains(user.SimpleName)))
            {
                _logger.LogDebug($"(suppressed) time out {user} for {duration} in #{_ircChannel}: {message}");
                return;
            }

            _logger.LogDebug($"time out {user} for {duration} in #{_ircChannel}: {message}");
            await Task.Run(() =>
                _twitchClient.TimeoutUser(_ircChannel, user.SimpleName, duration.ToTimeSpan(),
                    message ?? "no timeout reason was given"));
        }
    }
}
