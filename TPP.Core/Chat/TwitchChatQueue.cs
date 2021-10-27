using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Model;
using TwitchLib.Client;

namespace TPP.Core.Chat
{
    public record OutgoingMessage
    {
        private OutgoingMessage()
        {
            // Having a private constructor and all subtypes be sealed makes the set of all possible subtypes a
            // closed set, simulating a sum type.
        }

        public sealed record Chat(string Channel, string Message) : OutgoingMessage;
        public sealed record Reply(string Channel, string Message, string ReplyToId) : OutgoingMessage;
        public sealed record Whisper(string Receiver, string Message) : OutgoingMessage;
    }

    public class TwitchChatQueue
    {
        private static readonly Duration DefaultSleepDuration = Duration.FromMilliseconds(100);

        private readonly ILogger<TwitchChatQueue> _logger;
        private readonly TwitchClient _twitchClient;
        /// At which queue size messages get discarded.
        private readonly int _maxQueueLength;
        /// Shouldn't be smaller than either the minimum Task.Delay timer resolution
        /// or the supplied twitch client's internal throttle sleep value.
        private readonly Duration _sleepDuration;

        private readonly KeyCountPrioritizedQueue<string, OutgoingMessage> _queue = new();

        public TwitchChatQueue(
            ILogger<TwitchChatQueue> logger,
            TwitchClient twitchClient,
            int maxQueueLength = 100,
            Duration? sleepDuration = null)
        {
            _logger = logger;
            _twitchClient = twitchClient;
            _maxQueueLength = maxQueueLength;
            _sleepDuration = sleepDuration ?? DefaultSleepDuration;
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

        public Task StartSendWorker(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ProcessOne();
                    await Task.Delay(_sleepDuration.ToTimeSpan(), cancellationToken);
                }
            }, cancellationToken);
        }

        private void ProcessOne()
        {
            (string, OutgoingMessage)? kvp = _queue.Dequeue();
            if (kvp != null)
                Send(kvp.Value.Item2);
        }

        private void Send(OutgoingMessage message)
        {
            if (message is OutgoingMessage.Chat chat)
                _twitchClient.SendMessage(chat.Channel, chat.Message);
            else if (message is OutgoingMessage.Reply reply)
                _twitchClient.SendReply(reply.Channel, replyToId: reply.ReplyToId, message: reply.Message);
            else if (message is OutgoingMessage.Whisper whisper)
                _twitchClient.SendWhisper(whisper.Receiver, whisper.Message);
            else
                throw new ArgumentException("Unknown outgoing message type: " + message);
        }
    }
}
