using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Persistence.Models;
using Persistence.Repos;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace Core.Chat
{
    public sealed class TwitchChat : IChat
    {
        public event EventHandler<MessageEventArgs> IncomingMessage = null!;
        /// Twitch Messaging Interface (TMI, the somewhat IRC-compatible protocol twitch uses) maximum message length.
        /// This limit is in characters, not bytes. See https://discuss.dev.twitch.tv/t/message-character-limit/7793/6
        private const int MaxMessageLength = 500;
        private static readonly MessageSplitter MessageSplitterRegular = new MessageSplitter(
            maxMessageLength: MaxMessageLength - "/me ".Length);
        private static readonly MessageSplitter MessageSplitterWhisper = new MessageSplitter(
            // visual representation of the longest possible username (25 characters)
            maxMessageLength: MaxMessageLength - "/w ,,,,,''''',,,,,''''',,,,, ".Length);

        private readonly ILogger<TwitchChat> _logger;
        private readonly IClock _clock;
        private readonly string _ircChannel;
        private readonly ImmutableHashSet<ChatConfig.SuppressionType> _suppressions;
        private readonly ImmutableHashSet<string> _suppressionOverrides;
        private readonly IUserRepo _userRepo;
        private readonly TwitchClient _twitchClient;

        private bool _connected = false;
        private Action? _connectivityWorkerCleanup;

        public TwitchChat(
            ILoggerFactory loggerFactory,
            IClock clock,
            ChatConfig chatConfig,
            IUserRepo userRepo)
        {
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
        }

        public async Task SendMessage(string message)
        {
            if (_suppressions.Contains(ChatConfig.SuppressionType.Message) &&
                !_suppressionOverrides.Contains(_ircChannel))
            {
                _logger.LogDebug($"(suppressed) >#{_ircChannel}: {message}");
                return;
            }
            _logger.LogDebug($">#{_ircChannel}: {message}");
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
            if (_suppressions.Contains(ChatConfig.SuppressionType.Whisper) &&
                !_suppressionOverrides.Contains(target.SimpleName))
            {
                _logger.LogDebug($"(suppressed) >@{target.SimpleName}: {message}");
                return;
            }
            _logger.LogDebug($">@{target.SimpleName}: {message}");
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
            _twitchClient.OnMessageReceived += MessageReceived;
            _twitchClient.OnWhisperReceived += WhisperReceived;
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
            TimeSpan maxDelay = TimeSpan.FromMinutes(10);
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
                        _logger.LogError($"Failed to reconnect, trying again in {delay.TotalSeconds} seconds.");
                    }
                }

                await Task.Delay(delay, cancellationToken);
            }
        }

        private async void MessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            _logger.LogDebug($"<#{_ircChannel} {e.ChatMessage.Username}: {e.ChatMessage.Message}");
            await AnyMessageReceived(e.ChatMessage, e.ChatMessage.Message, MessageSource.Chat);
        }

        private async void WhisperReceived(object? sender, OnWhisperReceivedArgs e)
        {
            _logger.LogDebug($"<@{e.WhisperMessage.Username}: {e.WhisperMessage.Message}");
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
                color: string.IsNullOrEmpty(colorHex) ? null : colorHex.TrimStart('#'),
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
            _twitchClient.OnMessageReceived -= MessageReceived;
            _twitchClient.OnWhisperReceived -= WhisperReceived;
            _logger.LogDebug("twitch chat is now fully shut down.");
        }
    }
}
