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
        private static readonly MessageSplitter MessageSplitter = new MessageSplitter(maxMessageLength: 500);

        private readonly ILogger<TwitchChat> _logger;
        private readonly IClock _clock;
        private readonly string _ircChannel;
        private readonly ImmutableHashSet<IrcConfig.SuppressionType> _suppressions;
        private readonly ImmutableHashSet<string> _suppressionOverrides;
        private readonly IUserRepo _userRepo;
        private readonly TwitchClient _twitchClient;

        private bool _connected = false;

        public TwitchChat(
            ILoggerFactory loggerFactory,
            IClock clock,
            IrcConfig ircConfig,
            IUserRepo userRepo)
        {
            _logger = loggerFactory.CreateLogger<TwitchChat>();
            _clock = clock;
            _ircChannel = ircConfig.Channel;
            _suppressions = ircConfig.Suppressions;
            _suppressionOverrides = ircConfig.SuppressionOverrides
                .Select(s => s.ToLowerInvariant()).ToImmutableHashSet();
            _userRepo = userRepo;

            _twitchClient = new TwitchClient(
                client: new TcpClient(new ClientOptions()),
                logger: loggerFactory.CreateLogger<TwitchClient>());
            var credentials = new ConnectionCredentials(
                twitchUsername: ircConfig.Username,
                twitchOAuth: ircConfig.Password);
            _twitchClient.Initialize(
                credentials: credentials,
                channel: ircConfig.Channel,
                // disable TwitchLib's command features, we do that ourselves
                chatCommandIdentifier: '\0',
                whisperCommandIdentifier: '\0');
        }

        public Task SendMessage(string message)
        {
            if (_suppressions.Contains(IrcConfig.SuppressionType.Message) &&
                !_suppressionOverrides.Contains(_ircChannel))
            {
                _logger.LogDebug($"(suppressed) >#{_ircChannel}: {message}");
                return Task.CompletedTask;
            }
            _logger.LogDebug($">#{_ircChannel}: {message}");
            foreach (string part in MessageSplitter.FitToMaxLength(message))
            {
                _twitchClient.SendMessage(_ircChannel, part);
            }
            return Task.CompletedTask;
        }

        public Task SendWhisper(User target, string message)
        {
            if (_suppressions.Contains(IrcConfig.SuppressionType.Whisper) &&
                !_suppressionOverrides.Contains(target.SimpleName))
            {
                _logger.LogDebug($"(suppressed) >@{target.SimpleName}: {message}");
                return Task.CompletedTask;
            }
            _logger.LogDebug($">@{target.SimpleName}: {message}");
            foreach (string part in MessageSplitter.FitToMaxLength(message))
            {
                _twitchClient.SendWhisper(target.SimpleName, part);
            }
            return Task.CompletedTask;
        }

        private sealed class DelegateDisposer : IDisposable
        {
            private readonly Action _delegate;
            public DelegateDisposer(Action @delegate) => _delegate = @delegate;
            public void Dispose() => _delegate();
        }


        public IDisposable EstablishConnection()
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
            return new DelegateDisposer(() =>
            {
                tokenSource.Cancel();
                if (!checkConnectivityWorker.IsCanceled) checkConnectivityWorker.Wait();
                _twitchClient.Disconnect();
                _twitchClient.OnMessageReceived -= MessageReceived;
                _twitchClient.OnWhisperReceived -= WhisperReceived;
                _logger.LogDebug("twitch chat is now fully shut down.");
            });
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
            User user = await _userRepo.RecordUser(new UserInfo(
                id: twitchLibMessage.UserId,
                twitchDisplayName: twitchLibMessage.DisplayName,
                simpleName: twitchLibMessage.Username,
                color: twitchLibMessage.ColorHex,
                fromMessage: true,
                updatedAt: _clock.GetCurrentInstant()
            ));
            var message = new Message(user, messageText, source);
            IncomingMessage?.Invoke(this, new MessageEventArgs(message));
        }
    }
}
