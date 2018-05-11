using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using TPPCore.Irc;
using TPPCore.ChatProviders.DataModels;
using TPPCore.ChatProviders.Irc;

namespace TPPCore.ChatProviders.Providers.Irc
{
    public class IrcProvider : IProviderAsync
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const int keepAliveInterval = 60_000;
        private const int minReconnectDelay = 2_000;

        public string ClientName { get; private set; }
        public string ProviderName { get; protected set; } = "irc";

        private Object thisLock = new Object();
        protected ProviderContext context;
        protected IrcClient ircClient;
        protected TcpClient tcpClient;
        private StreamReader reader;
        private StreamWriter writer;
        private ConnectConfig connectConfig;
        private bool isRunning = false;

        public IrcProvider()
        {
        }

        public void Configure(string clientName, ProviderContext providerContext)
        {
            this.ClientName = clientName;
            this.context = providerContext;

            connectConfig = new ConnectConfig() {
                Host = context.Service.ConfigReader.GetCheckedValue<string>(
                    "chat", "clients", ClientName, "host"),
                Port = context.Service.ConfigReader.GetCheckedValue<int>(
                    "chat", "clients", ClientName, "port"),
                Ssl = context.Service.ConfigReader.GetCheckedValue<bool>(
                    "chat", "clients", ClientName, "ssl"),
                Timeout = context.Service.ConfigReader.GetCheckedValue<int>(
                    "chat", "clients", ClientName, "timeout"),
                Nickname = context.Service.ConfigReader.GetCheckedValue<string>(
                    "chat", "clients", ClientName, "nickname"),
                Password = context.Service.ConfigReader.GetCheckedValueOrDefault<string>(
                    new[] {"chat", "clients", ClientName, "password"}, null),
                Channels = context.Service.ConfigReader.GetCheckedValue<string[]>(
                    "chat", "clients", ClientName, "channels"),
                MaxMessageBurst = context.Service.ConfigReader.GetCheckedValue<int>(
                    "chat", "clients", ClientName, "rateLimit", "maxMessageBurst"),
                CounterPeriod = context.Service.ConfigReader.GetCheckedValue<int>(
                    "chat", "clients", ClientName, "rateLimit", "counterPeriod"),
            };
        }

        public async Task Run()
        {
            isRunning = true;

            while (isRunning)
            {
                if (!(await connectAndLogin()))
                {
                    // FIXME: use backoff
                    await Task.Delay(60_000);
                    continue;
                }

                await loopForever();
                await cleanUpIrcClient();
                logger.Info("Disconnected.");

                await Task.Delay(minReconnectDelay);
            }
        }

        public void Shutdown()
        {
            isRunning = false;

            if (ircClient != null)
            {
                ircClient.SendMessage("QUIT").Wait();
            }
        }

        protected virtual IrcClient newIrcClient()
        {
            return new IrcClient(reader, writer);
        }

        private void initIrcClient()
        {
            ircClient = newIrcClient();
            ircClient.EnableChannelTracker()
                .EnableCtcpPingVersion()
                .EnablePingHandler();
            ircClient.RateLimiter = new TokenBucketRateLimiter(
                connectConfig.MaxMessageBurst, connectConfig.CounterPeriod);

            addDebuggingHandlers();
            addRawLineHandlers();
        }

        private void addDebuggingHandlers()
        {
            ircClient.MessageReceived += (IrcClient client, Message message) =>
            {
                logger.DebugFormat("IRC RECV: {0}", message.Raw);
                return Task.CompletedTask;
            };

            ircClient.MessageSending += (IrcClient client, Message message) =>
            {
                logger.DebugFormat("IRC SEND: {0}", message.Raw);
                return Task.CompletedTask;
            };

            ircClient.MessageReceived += (IrcClient client, Message message) =>
            {
                var code = message.NumericReply;
                if (code >= 400 && code <= 599)
                {
                    logger.ErrorFormat("IRC client got back error {0} {1}",
                        code, string.Join(" ", message.Parameters));
                }
                return Task.CompletedTask;
            };
        }

        private async Task cleanUpIrcClient()
        {
            if (ircClient == null)
            {
                return;
            }

            if (tcpClient.Connected)
            {
                await ircClient.SendMessage("QUIT");
                tcpClient.Close();
            }

            reader.Dispose();
            writer.Dispose();
            ircClient = null;
            reader = null;
            writer = null;
        }

        private async Task<bool> connectAndLogin()
        {
            try
            {
                await connect();
                initIrcClient();
                await login();
            }
            catch (Exception error)
            when (error is SocketException
            || error is AuthenticationException
            || error is IrcTimeoutException)
            {
                logger.Error("Connection failure.", error);
                await cleanUpIrcClient();
                return false;
            }

            return true;
        }

        private async Task connect()
        {
            tcpClient = new TcpClient();
            tcpClient.ReceiveTimeout = tcpClient.SendTimeout
                = connectConfig.Timeout;
            tcpClient.NoDelay = true;

            var host = connectConfig.Host;
            var port = connectConfig.Port;

            logger.InfoFormat("Connecting to {0}:{1}...", host, port);

            await tcpClient.ConnectAsync(host, port);

            logger.Info("Connected.");

            var stream = tcpClient.GetStream();

            if (connectConfig.Ssl)
            {
                var sslStream = new SslStream(stream);

                logger.Info("Establishing SSL...");
                await sslStream.AuthenticateAsClientAsync(host);
                logger.Info("SSL established.");

                reader = new StreamReader(sslStream);
                writer = new StreamWriter(sslStream);
            }
            else
            {
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);
            }
        }

        protected virtual async Task login()
        {
            var nickname = connectConfig.Nickname;

            logger.InfoFormat("Logging in as {0}", nickname);
            await ircClient.Register(nickname, nickname, nickname,
                connectConfig.Password);

            await ircClient.WaitReply(NumericalReplyCodes.RPL_WELCOME);
            logger.Info("Logged in.");
        }

        private async Task loopForever()
        {
            try
            {
                setUpEventHandlers();
                await joinChannels();

                while (isRunning && tcpClient.Connected)
                {
                    try
                    {
                        await ircClient.ProcessOnce(keepAliveInterval);
                    }
                    catch (IrcTimeoutException)
                    {
                        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        await ircClient.SendMessage("PING", null, timestamp.ToString());
                    }
                }
            }
            catch (Exception error)
            when (error is SocketException || error is IrcConnectionException)
            {
                logger.Error("Unexpectedly disconnected.", error);
            }
        }

        protected virtual void setUpEventHandlers()
        {
            ircClient.CommandHandlers.AddOrCombine("JOIN", joinedEventHandler);
            ircClient.CommandHandlers.AddOrCombine("PART", partedEventHandler);
            ircClient.CommandHandlers.AddOrCombine("PRIVMSG", messageEventHandler);
            ircClient.CommandHandlers.AddOrCombine("NOTICE", noticeEventHandler);
        }

        private void addRawLineHandlers()
        {
            ircClient.MessageReceived += (sender, message) =>
            {
                var rawEvent = new RawContentEvent()
                {
                    ClientName = ClientName,
                    ProviderName = ProviderName,
                    RawContent = message.Raw
                };
                context.PublishChatEvent(rawEvent);
                return Task.CompletedTask;
            };

            ircClient.MessageSending += (sender, message) =>
            {
                var rawEvent = new RawContentEvent()
                {
                    ClientName = ClientName,
                    ProviderName = ProviderName,
                    RawContent = message.Raw,
                    IsSelf = true
                };
                context.PublishChatEvent(rawEvent);
                return Task.CompletedTask;
            };
        }

        private async Task joinChannels()
        {
            await ircClient.Join(connectConfig.Channels);
            // TODO: handle case where join failed due to temporary failure
            // such as netsplits
        }

        public Task<IList<ChatUser>> GetRoomList(string channel)
        {
            var ircChannel = ircClient.ChannelTracker[channel];

            var users = new List<ChatUser>();

            foreach (var ircUser in ircChannel.Values)
            {
                var user = ircUser.ClientId.ToChatUserModel();
                users.Add(user);
            }

            return Task.FromResult((IList<ChatUser>)users);
        }

        public string GetUserId()
        {
            return ircClient.ClientId.ToString();
        }

        public string GetUsername()
        {
            return ircClient.ClientId.NicknameLower;
        }

        public async Task SendMessage(string channel, string message)
        {
            if (!channel.IsChannel())
            {
                throw new Exception("Not a channel");
            }
            await ircClient.Privmsg(channel, message);
        }

        public async Task SendPrivateMessage(string user, string message)
        {
            if (user.IsChannel())
            {
                throw new Exception("Not a user");
            }
            await ircClient.Privmsg(user, message);
        }

        public async Task TimeoutUser(string user, string reason, int duration, string channel)
        {
            if (!channel.IsChannel())
            {
                throw new Exception("Not a channel");
            }
            //TODO: add timeouts in IRC
            throw new NotImplementedException();
        }

        public async Task BanUser(string user, string reason, string channel)
        {
            if (!channel.IsChannel())
            {
                throw new Exception("Not a channel");
            }
            //TODO: add bans in IRC
            throw new NotImplementedException();
        }

        private Task joinedEventHandler(IrcClient client, Message message)
        {
            if (client.ClientId.NicknameEquals(message.Prefix.ClientId))
            {
                logger.InfoFormat("Client joined channel {0}", message.TargetLower);
            }

            publishJoin(message);

            return Task.CompletedTask;
        }

        private Task partedEventHandler(IrcClient client, Message message)
        {
            if (client.ClientId.NicknameEquals(message.Prefix.ClientId))
            {
                logger.InfoFormat("Client parted channel {0}", message.TargetLower);
            }

            publishPart(message);

            return Task.CompletedTask;
        }

        private Task messageEventHandler(IrcClient client, Message message)
        {
            publishMessage(message, false);

            return Task.CompletedTask;
        }

        private Task noticeEventHandler(IrcClient client, Message message)
        {
            publishMessage(message, true);

            return Task.CompletedTask;
        }

        private void publishJoin(Message message)
        {
            var chatEvent = new UserEvent() {
                ClientName = ClientName,
                ProviderName = ProviderName,
                EventType = UserEventTypes.Join,
                Channel = message.TargetLower,
                User = getMessageSender(message),
                IsSelf = ircClient.ClientId.NicknameEquals(message.Prefix.ClientId)
            };
            context.PublishChatEvent(chatEvent);
        }

        private void publishPart(Message message)
        {
            var chatEvent = new UserEvent() {
                ClientName = ClientName,
                ProviderName = ProviderName,
                EventType = UserEventTypes.Part,
                Channel =  message.TargetLower,
                User = getMessageSender(message),
                IsSelf = isMessageSelf(message)
            };
            context.PublishChatEvent(chatEvent);
        }

        private void publishMessage(Message message, bool isNotice)
        {
            var chatMessage = new ChatMessage() {
                ClientName = ClientName,
                ProviderName = ProviderName,
                TextContent = message.TrailingParameter,
                Channel = message.TargetLower,
                IsNotice = isNotice,
                Sender = getMessageSender(message),
                IsSelf = isMessageSelf(message)
            };
            message.Tags.ToList().ForEach(item => chatMessage.Meta.Add(item));

            context.PublishChatEvent(chatMessage);
        }

        protected virtual bool isMessageSelf(Message message)
        {
            return ircClient.ClientId.NicknameEquals(message.Prefix.ClientId);
        }

        protected virtual ChatUser getMessageSender(Message message)
        {
            return message.Prefix.ClientId != null
                ? message.Prefix.ClientId.ToChatUserModel()
                : null;
        }
    }
}
