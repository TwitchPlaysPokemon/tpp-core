using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Model;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;

namespace TPP.Core.Chat
{
    public record OutgoingMessage
    {
        private OutgoingMessage()
        {
            // Having a private constructor and all subtypes be sealed makes the set of all possible subtypes a
            // closed set, simulating a sum type.
        }

        public sealed record Chat(string ChannelId, string Message) : OutgoingMessage;
        public sealed record Reply(string ChannelId, string Message, string ReplyToId) : OutgoingMessage;
        public sealed record Whisper(string Receiver, string Message, bool NewRecipient) : OutgoingMessage;
    }

    public sealed class TwitchChatQueue : IAsyncDisposable
    {
        private static readonly Duration DefaultSleepDuration = Duration.FromMilliseconds(100);

        private readonly ILogger<TwitchChatQueue> _logger;
        private readonly string _senderUserId;
        private readonly TwitchApiProvider _twitchApiProvider;
        /// At which queue size messages get discarded.
        private readonly int _maxQueueLength;
        /// Shouldn't be smaller than either the minimum Task.Delay timer resolution
        /// or the supplied twitch client's internal throttle sleep value.
        private readonly Duration _sleepDuration;

        private readonly KeyCountPrioritizedQueue<string, OutgoingMessage> _queue = new();

        private readonly CancellationTokenSource _cancellationToken;
        private readonly Task _sendWorker;

        public TwitchChatQueue(
            ILogger<TwitchChatQueue> logger,
            string senderUserId,
            TwitchApiProvider twitchApiProvider,
            int maxQueueLength = 100,
            Duration? sleepDuration = null)
        {
            _logger = logger;
            _senderUserId = senderUserId;
            _twitchApiProvider = twitchApiProvider;
            _maxQueueLength = maxQueueLength;
            _sleepDuration = sleepDuration ?? DefaultSleepDuration;
            _cancellationToken = new CancellationTokenSource();
            _sendWorker = Task.Run(() => SendWorker(_cancellationToken.Token));
        }

        public void Enqueue(User? user, OutgoingMessage message)
        {
            _queue.Enqueue(user?.Id ?? string.Empty, message);
            while (_queue.Count > _maxQueueLength)
            {
                (string, OutgoingMessage) discarded = _queue.DequeueLast()!.Value;
                _logger.LogDebug(
                    "Outgoing message queue is full (size {Capacity})! Discarded message from user {User}: {Message}",
                    _maxQueueLength, discarded.Item1, discarded.Item2);
            }
        }

        private async Task SendWorker(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ProcessOne();
                try { await Task.Delay(_sleepDuration.ToTimeSpan(), cancellationToken); }
                catch(OperationCanceledException) {}
            }
        }

        private async Task ProcessOne()
        {
            (string, OutgoingMessage)? kvp = _queue.Dequeue();
            if (kvp == null)
                return;
            try
            {
                await Send(kvp.Value.Item2);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while processing twitch chat queue item {Item}", kvp.Value);
            }
        }

        private async Task Send(OutgoingMessage message)
        {
            TwitchAPI twitchApi = await _twitchApiProvider.Get();
            if (message is OutgoingMessage.Chat chat)
                await twitchApi.Helix.Chat.SendChatMessage(chat.ChannelId, _senderUserId, chat.Message);
            else if (message is OutgoingMessage.Reply reply)
                // TODO https://github.com/TwitchLib/TwitchLib.Api/pull/386
                await twitchApi.Helix.Chat.SendChatMessage(reply.ChannelId, _senderUserId, reply.Message /*, replyParentMessageId: reply.ReplyToId*/);
            else if (message is OutgoingMessage.Whisper whisper)
                await SendWhisperCatchingSomeErrors(twitchApi, whisper);
            else
                throw new ArgumentException("Unknown outgoing message type: " + message);
        }

        private async Task SendWhisperCatchingSomeErrors(TwitchAPI twitchApi, OutgoingMessage.Whisper whisper)
        {
            try
            {
                await twitchApi.Helix.Whispers.SendWhisperAsync(
                    _senderUserId,
                    whisper.Receiver,
                    whisper.Message,
                    whisper.NewRecipient
                );
            }
            catch (HttpResponseException e)
            {
                string response = await e.HttpResponse.Content.ReadAsStringAsync();
                if (e.HttpResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    // Trying to whisper people can fail for mostly two reasons:
                    // - The user has blocked the bot, which frequently happens.
                    //   On a side note, we might want to stop whispering people for stuff like pinball drops,
                    //   at lest until we know somehow they want that, e.g. because they whispered us first.
                    // - The user has not whispered us before and has "Block whispers from strangers" enabled.
                    // In both cases there's nothing we can do about that, so let's just ignore the failure.
                    _logger.LogDebug(e,
                        "Ignoring 403 whisper failure to {Receiver}. " +
                        "New Recipient: {NewRecipient}, Response: '{Response}'. Whisper: '{Message}'",
                        whisper.Receiver, whisper.NewRecipient, response, whisper.Message);
                }
                else
                {
                    throw new Exception(
                        $"Error while sending whisper to {whisper.Receiver}. " +
                        $"New Recipient: {whisper.NewRecipient}, Status Code: {e.HttpResponse.StatusCode}, " +
                        $"Response: '{response}'. Whisper: '{whisper.Message}'", e);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _cancellationToken.CancelAsync();
            await _sendWorker;
        }
    }
}
