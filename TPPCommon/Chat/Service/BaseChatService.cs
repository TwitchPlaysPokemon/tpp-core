using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TPPCommon.Chat;
using TPPCommon.Configuration;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TPPCommon.Chat.Service {
    /// <summary>
    /// Chat service for handling, sending, and dispatching messages to PubSub
    /// and the chat client.
    /// </summary>
    public abstract class BaseChatService : TPPService {
        protected TPPLoggerBase Logger;
        protected ExponentialBackoffCounter backoff;
        protected Client.IChatClient ChatClient;
        protected Client.ConnectionConfig ConnectionConfig;
        protected abstract string ServiceName { get; }

        protected override string[] ConfigNames
        {
            get => new string[] { $"config_{ServiceName}" };
        }
        protected override int StartupDelayMilliseconds
        {
            get => 0;
        }

        public BaseChatService(
                IPublisher publisher,
                ISubscriber subscriber,
                ITPPLoggerFactory loggerFactory,
                IConfigReader configReader) :
                base (publisher, subscriber, loggerFactory, configReader)
        {
            backoff = new ExponentialBackoffCounter();
        }

        override protected void Initialize()
        {
            Logger = LoggerFactory.Create($"chat.{ServiceName}");
        }

        override protected void Run()
        {
            while (true) {
                Connect().Wait();

                if (!ChatClient.IsConnected())
                {
                    continue;
                }

                ProcessMessagesAsync().Wait();

                Logger.LogInfo("Chat service disconnected");
                backoff.Increment();
                backoff.Sleep();
            }
        }

        public void InitializeClient() {
            Initialize();
        }

        public async Task Connect()
        {
            Logger.LogInfo("Chat service connecting");

            try {
                await ChatClient.ConnectAsync(ConnectionConfig);
            } catch (SocketException error) {
                Logger.LogError("Error connecting to chat", error);

                backoff.Increment();
                await backoff.SleepAsync();
            }

            Logger.LogInfo("Chat service connected");
            backoff.Reset();
        }

        public async Task ProcessMessagesAsync()
        {
            Logger.LogDebug("Processing messages");

            var sendTask = SendMessagesAsync();
            var receiveTask = ReceiveMessagesAsync();
            Task[] tasks = {sendTask, receiveTask};
            await Task.WhenAny(tasks);

            ChatClient.Disconnect();

            Logger.LogDebug("Stopped processing messages");
        }

        public async Task SendMessagesAsync()
        {
            Logger.LogDebug("Processing sending messages");

            while (ChatClient.IsConnected()) {
                // TODO: read messages from a queue
                await Task.Delay(1000);
            }

            Logger.LogDebug("Stopped sending messages");
        }

        public async Task ReceiveMessagesAsync()
        {
            Logger.LogDebug("Processing receiving messages");

            while (ChatClient.IsConnected()) {
                await ReceiveOneMessageAsync();
            }

            Logger.LogDebug("Stopped receiving messages");
        }

        public async Task ReceiveOneMessageAsync()
        {
            var message = await ChatClient.ReceiveMessageAsync();
            ChatMessageEvent chatEvent =
                new ChatMessageEvent(ServiceName, message);
            Publisher.Publish(chatEvent);
        }
    }
}
