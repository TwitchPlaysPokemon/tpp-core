using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace TPPCore.Irc
{
    /// <summary>
    /// An async event handler that processes a message.
    /// </summary>
    public delegate Task IrcClientMessageEventHandler(IrcClient client, Message message);

    /// <summary>
    /// Low level, network independent, task-based asynchronous IRC client.
    /// </summary>
    /// <remarks>
    /// <para>This is a very flexible IRC client handling the IRC protocol at
    /// a low level.</para>
    /// <para>There is no connect method. Instead, the streams can
    /// be provided from a connected TcpClient or text streams for testing.</para>
    /// <para>There is no background loop. Instead, the user must pump events
    /// by manually calling the event processing methods.</para>
    /// </remarks>
    public class IrcClient
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Async event handler when a message is received.
        /// </summary>
        public event IrcClientMessageEventHandler MessageReceived;

        /// <summary>
        /// Async event handler when a message is to be sent.
        /// </summary>
        public event IrcClientMessageEventHandler MessageSending;

        /// <summary>
        /// Rate limiter to throttle how fast messages are sent.
        /// </summary>
        public IRateLimiter RateLimiter;

        /// <summary>
        /// Async event handlers for the corresponding command.
        /// </summary>
        public readonly Dictionary<string,IrcClientMessageEventHandler> CommandHandlers;

        /// <summary>
        /// Async event handlers for the corresponding numerical reply.
        /// </summary>
        public readonly Dictionary<int,IrcClientMessageEventHandler> NumericReplyHandlers;

        /// <summary>
        /// Channel tracker if enabled.
        /// </summary>
        /// <remarks>
        /// This value is null unless <see cref="EnableChannelTracker"/>
        /// is called.
        /// </remarks>
        public ChannelTracker ChannelTracker { get; private set; }

        /// <summary>
        /// Our Client ID.
        /// </summary>
        /// <remarks>
        /// The values in the Client ID are null until we receive them from
        /// the server.
        /// </remarks>
        public ClientId ClientId;

        private StreamReader reader;
        private StreamWriter writer;
        private Task<Message> pendingReadTask;

        public IrcClient(StreamReader reader, StreamWriter writer)
        {
            this.reader = reader;
            this.writer = writer;
            this.CommandHandlers = new Dictionary<string,IrcClientMessageEventHandler>();
            this.NumericReplyHandlers = new Dictionary<int,IrcClientMessageEventHandler>();
            this.ClientId = new ClientId();

            NumericReplyHandlers.AddOrCombine(NumericalReplyCodes.RPL_WELCOME,
                (client, message) =>
                {
                    if (message.Target != null)
                    {
                        ClientId.Nickname = message.Target;
                    }
                    return Task.CompletedTask;
                }
            );
        }

        /// <summary>
        /// Read a single message and dispatch the event handlers.
        /// </summary>
        /// <param name="timeout">Time in milliseconds to timeout and throw
        /// <see cref="IrcTimeoutException"/>. Non-positive values are
        /// treated as no timeout.</param>
        public async Task<Message> ProcessOnce(int timeout = 0)
        {
            var readTask = pendingReadTask = pendingReadTask ?? ReadMessage();

            if (timeout > 0)
            {
                var cancelSource = new CancellationTokenSource();
                var timeoutTask = Task.Delay(timeout, cancelSource.Token);

                if (await Task.WhenAny(readTask, timeoutTask) == readTask)
                {
                    cancelSource.Cancel();
                }
                else
                {
                    throw new IrcTimeoutException();
                }
            }

            var message = await readTask;
            pendingReadTask = null;

            await dispatchCommandHandlers(message);
            await dispatchNumericReplyHandlers(message);

            return message;
        }

        private async Task dispatchCommandHandlers(Message message)
        {
            if (message.Command == null
            || !CommandHandlers.ContainsKey(message.Command))
            {
                return;
            }
            var handlers = CommandHandlers[message.Command];

            foreach (var handler in handlers.GetInvocationList())
            {
                await ((IrcClientMessageEventHandler) handler)(this, message);
            }
        }

        private async Task dispatchNumericReplyHandlers(Message message)
        {
            if (message.NumericReply < 0
            || !NumericReplyHandlers.ContainsKey(message.NumericReply))
            {
                return;
            }
            var handlers = NumericReplyHandlers[message.NumericReply];

            foreach (var handler in handlers.GetInvocationList())
            {
                await ((IrcClientMessageEventHandler) handler)(this, message);
            }
        }

        /// <summary>
        /// Read and return a single message.
        /// </summary>
        public async Task<Message> ReadMessage()
        {
            string line;
            try
            {
                line = await reader.ReadLineAsync();
            }
            catch (ArgumentOutOfRangeException error)
            {
                throw new IrcException("Line too long", error);
            }

            if (line == null)
            {
                throw new IrcConnectionException("Stream has ended.");
            }

            var message = new Message();
            message.ParseFrom(line);

            await dispatchReceiveHandler(message);

            return message;
        }

        private async Task dispatchReceiveHandler(Message message)
        {
            if (MessageReceived == null)
            {
                return;
            }

            foreach (var handler in MessageReceived.GetInvocationList())
            {
                await ((IrcClientMessageEventHandler) handler)(this, message);
            }
        }

        /// <summary>
        /// Send a message.
        /// </summary>
        public async Task SendMessage(Message message)
        {
            Debug.Assert(message.Raw == null);

            if (RateLimiter != null)
            {
                var waitTime = RateLimiter.GetWaitTime();

                if (waitTime > 0) {
                    await Task.Delay(waitTime);
                }
            }

            message.Raw = message.ToString();

            await dispatchSendingHandler(message);
            await writer.WriteLineAsync(message.Raw);
            await writer.FlushAsync();

            if (RateLimiter != null)
            {
                RateLimiter.Increment();
            }
        }

        private async Task dispatchSendingHandler(Message message)
        {
            if (MessageSending == null)
            {
                return;
            }
            foreach (var handler in MessageSending.GetInvocationList())
            {
                await ((IrcClientMessageEventHandler) handler)(this, message);
            }
        }

        /// <summary>
        /// Add an event handler that will respond to PING with PONG.
        /// </summary>
        public IrcClient EnablePingHandler()
        {
            CommandHandlers.AddOrCombine("PING", pingHandler);
            return this;
        }

        private static async Task pingHandler(IrcClient client, Message message)
        {
            await client.SendMessage(new Message(
                "PONG", null, message.TrailingParameter));
        }

        /// <summary>
        /// Add an event handler that will respond TO CTCP PING and VERSION.
        /// </summary>
        public IrcClient EnableCtcpPingVersion()
        {
            CommandHandlers.AddOrCombine("PRIVMSG", ctcpPingVersionHandler);
            return this;
        }

        private static async Task ctcpPingVersionHandler(IrcClient client,
        Message message)
        {
            if (message.Prefix.ClientId == null
            || !message.Parameters.Any()
            || message.Parameters[0].IsChannel())
            {
                // Allow only private messages to us.
                return;
            }
            var ctcpMessage = message.CtcpMessage;
            CtcpMessage replyCtcpMessage = null;

            switch (ctcpMessage.Command)
            {
                case "PING":
                    replyCtcpMessage = ctcpMessage;
                    break;
                case "VERSION":
                    replyCtcpMessage = new CtcpMessage("VERSION", "TPPCore");
                    break;
            }

            if (replyCtcpMessage != null)
            {
                var reply = new Message(
                    "NOTICE",
                    new[] {message.Prefix.ClientId.Nickname},
                    replyCtcpMessage
                );
                await client.SendMessage(reply);
            }
        }

        public IrcClient EnableChannelTracker()
        {
            ChannelTracker = new ChannelTracker();
            MessageReceived += channelTrackerHandler;
            return this;
        }

        private Task channelTrackerHandler(object sender, Message message)
        {
            ChannelTracker.UpdateFromMessage(message);
            return Task.CompletedTask;
        }
    }


    public static class IrcClientHandlerExtensions
    {
        public static void AddOrCombine<K>(
        this Dictionary<K,IrcClientMessageEventHandler> dict,
        K key, IrcClientMessageEventHandler value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] += value;
            }
            else
            {
                dict[key] = value;
            }
        }
    }
}
