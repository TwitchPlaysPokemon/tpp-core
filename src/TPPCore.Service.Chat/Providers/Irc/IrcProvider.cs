using IrcDotNet;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TPPCore.Utils;
using System;
using TPPCore.Service.Chat.DataModels;
using TPPCore.Service.Chat.Irc;

namespace TPPCore.Service.Chat.Providers.Irc
{
    public class IrcProvider : IProviderThreaded
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const int keepAliveInterval = 60_000;

        public string Name { get { return "irc"; } }

        private Object thisLock = new Object();
        private ProviderContext context;
        private StandardIrcClient ircClient;
        private ConnectConfig connectConfig;
        private bool isRunning = false;
        private Timer keepAliveTimer;

        public IrcProvider()
        {
            keepAliveTimer = new Timer(state => keepAliveTimerCallback(),
                null,
                Timeout.Infinite, Timeout.Infinite);
        }

        public void Configure(ProviderContext providerContext)
        {
            this.context = providerContext;

            connectConfig = new ConnectConfig() {
                Host = context.Service.ConfigReader.GetCheckedValue<string>(
                    new[] {"chat", Name, "host"}),
                Port = context.Service.ConfigReader.GetCheckedValue<int>(
                    new[] {"chat", Name, "port"}),
                Ssl = context.Service.ConfigReader.GetCheckedValue<bool>(
                    new[] {"chat", Name, "ssl"}),
                Timeout = context.Service.ConfigReader.GetCheckedValue<int>(
                    new[] {"chat", Name, "timeout"}),
                Nickname = context.Service.ConfigReader.GetCheckedValue<string>(
                    new[] {"chat", Name, "nickname"}),
                Password = context.Service.ConfigReader.GetCheckedValueOrDefault<string>(
                    new[] {"chat", Name, "password"}, null),
                Channels = context.Service.ConfigReader.GetCheckedValue<string[]>(
                    new[] {"chat", Name, "channels"}),
                MaxMessageBurst = context.Service.ConfigReader.GetCheckedValue<int>(
                    new[] {"chat", Name, "rateLimit", "maxMessageBurst"}),
                CounterPeriod = context.Service.ConfigReader.GetCheckedValue<int>(
                    new[] {"chat", Name, "rateLimit", "counterPeriod"}),
            };
        }

        public void Run()
        {
            isRunning = true;
            keepAliveTimer.Change(0, keepAliveInterval);

            while (isRunning)
            {
                initIrcClient();

                var disconnectEvent = connectAndWaitLogin();

                if (ircClient.IsRegistered)
                {
                    logger.Info("Logged in.");
                    setUpEventHandlers();
                    joinChannels();
                    disconnectEvent.Wait();
                    logger.Info("Disconnected.");
                }
                else
                {
                    logger.Error("IRC client couldn't connect and log in for some reason.");
                    // FIXME: use backoff
                    Thread.Sleep(60000);
                }

                cleanUpIrcClient();

                Thread.Sleep(2000);
            }
        }

        public void Shutdown()
        {
            isRunning = false;

            cleanUpIrcClient();

            if (keepAliveTimer != null)
            {
                keepAliveTimer.Dispose();
                keepAliveTimer = null;
            }
        }

        private void initIrcClient()
        {
            ircClient = new StandardIrcClient();
            ircClient.FloodPreventer = new IrcStandardFloodPreventer(
                connectConfig.MaxMessageBurst, connectConfig.CounterPeriod);

            ircClient.RawMessageReceived +=
                (object sender, IrcRawMessageEventArgs args) =>
                    logger.DebugFormat("IRC RECV: {0}", args.RawContent);

            ircClient.RawMessageSent +=
                (object sender, IrcRawMessageEventArgs args) =>
                    logger.DebugFormat("IRC SEND: {0}", args.RawContent);

            ircClient.Error +=
                (object sender, IrcErrorEventArgs args) =>
                    logger.Error("IRC client exception", args.Error);

            ircClient.ProtocolError +=
                (object sender, IrcProtocolErrorEventArgs args) =>
                    logger.ErrorFormat("IRC client got back error {0} {1} {2}",
                        args.Code, string.Join(", ", args.Parameters),
                        args.Message);

            addRawLineHandlers();
        }

        private void cleanUpIrcClient()
        {
            lock (thisLock)
            {
                if (ircClient == null)
                {
                    return;
                }

                try
                {
                    if (ircClient.IsConnected)
                    {
                        ircClient.Quit(connectConfig.Timeout, "");
                        ircClient.Disconnect();
                    }

                    ircClient.Dispose();
                }
                catch (System.ObjectDisposedException)
                {
                    // Ignore it. The client can be disposed whenever it wants
                    // to be :P
                }
                ircClient = null;
            }
        }

        private ManualResetEventSlim connectAndWaitLogin()
        {
            var disconnectEvent = new ManualResetEventSlim();

            using (var registerEvent = new ManualResetEventSlim())
            {
                using (var connectEvent = new ManualResetEventSlim())
                {
                    ircClient.Connected += (sender, args) => connectEvent.Set();
                    ircClient.ConnectFailed += (sender, args) => connectEvent.Set();
                    ircClient.Registered += (sender, args) => registerEvent.Set();
                    ircClient.Disconnected += (sender, args) => disconnectEvent.Set();

                    connect();

                    connectEvent.Wait(connectConfig.Timeout);
                }

                if (!ircClient.IsConnected)
                {
                    logger.Error("Connect timed out.");
                    ircClient.Disconnect();
                    return disconnectEvent;
                }

                logger.Info("Connected. Logging in...");
                var success = registerEvent.Wait(connectConfig.Timeout);

                if (!success)
                {
                    logger.Error("Login timed out.");
                }
            }

            return disconnectEvent;
        }

        private void connect()
        {
            var regInfo = new IrcUserRegistrationInfo() {
                NickName = connectConfig.Nickname,
                UserName = connectConfig.Nickname,
                Password = connectConfig.Password,
                RealName = connectConfig.Nickname,
                UserModes = new[] {'i'}
            };

            logger.InfoFormat("Connecting to {0}:{1} as {2}",
                connectConfig.Host, connectConfig.Port, connectConfig.Nickname);

            ircClient.Connect(connectConfig.Host, connectConfig.Port,
                connectConfig.Ssl, regInfo);
        }

        private void keepAliveTimerCallback()
        {
            lock (thisLock)
            {
                if (ircClient == null || !ircClient.IsRegistered)
                {
                    return;
                }

                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                // FIXME: calling this results in the RawMessageSent event handler
                // return null for args
                ircClient.SendRawMessage($"PING :{timestamp}");
            }
        }

        private void setUpEventHandlers()
        {
            ircClient.LocalUser.JoinedChannel += localUserJoinedEventHandler;
            ircClient.LocalUser.LeftChannel += localUserPartedEventHandler;
            ircClient.LocalUser.MessageReceived += localUserMessageEventHandler;
            ircClient.LocalUser.NoticeReceived += localUserNoticeEventHandler;
        }

        private void addRawLineHandlers()
        {
            ircClient.RawMessageReceived += (sender, args) =>
            {
                var rawEvent = new RawContentEvent()
                {
                        RawContent = args.RawContent
                };
                context.PublishChatEvent(rawEvent);
            };
        }

        private void joinChannels()
        {
            ircClient.Channels.Join(connectConfig.Channels);
            // TODO: handle case where join failed due to temporary failure
            // such as netsplits
        }

        public IList<ChatUser> GetRoomList(string channel)
        {
            var ircChannel = ircClient.Channels
                .Where(item => item.Name.ToLowerIrc() == channel.ToLowerIrc())
                .FirstOrDefault();

            var users = new List<ChatUser>();

            foreach (var ircUser in ircChannel.Users)
            {
                var user = ircUser.User.ToChatUserModel();
                users.Add(user);
            }

            return users;
        }

        public string GetUserId()
        {
            return ircClient.LocalUser.UserName;
        }

        public void SendMessage(string channel, string message)
        {
            channel.CheckUnsafeChars();
            message.CheckUnsafeChars();
            // FIXME: check if we are actually sending to a channel
            ircClient.LocalUser.SendMessage(channel, message);
        }

        public void SendPrivateMessage(string user, string message)
        {
            user.CheckUnsafeChars();
            message.CheckUnsafeChars();
            // FIXME: check if we are actually sending to a user;
            ircClient.LocalUser.SendMessage(user, message);
        }

        private void localUserJoinedEventHandler(object sender, IrcChannelEventArgs args)
        {
            logger.InfoFormat("Client joined channel {0}", args.Channel);

            var localUser = (IrcLocalUser) sender;
            addHandlersToChannel(args.Channel);
        }

        private void localUserPartedEventHandler(object sender, IrcChannelEventArgs args)
        {
            logger.InfoFormat("Client parted channel {0}", args.Channel);

            var localUser = (IrcLocalUser) sender;
            removeHandlersFromChannel(args.Channel);
        }

        private void localUserMessageEventHandler(object sender, IrcMessageEventArgs args)
        {
            publishLocalUserMessage(args, false);
        }

        private void localUserNoticeEventHandler(object sender, IrcMessageEventArgs args)
        {
            publishLocalUserMessage(args, true);
        }

        private void publishLocalUserMessage(IrcMessageEventArgs args, bool isNotice)
        {
            var chatMessage = new ChatMessage() {
                ProviderName = Name,
                TextContent = args.Text,
                IsNotice = isNotice,
            };

            // Received a private message from a user or a server.
            if (args.Source is IrcUser)
            {
                var ircUser = args.Source as IrcUser;
                chatMessage.Sender = ircUser.ToChatUserModel();
            }

            context.PublishChatEvent(chatMessage);
        }

        private void addHandlersToChannel(IrcChannel channel) {
            channel.UserJoined += userJoinedChannelEventHandler;
            channel.UserLeft += userPartedChannelEventHandler;
            channel.MessageReceived += channelMessageEventHandler;
            channel.NoticeReceived += channelNoticeEventHandler;
        }

        private void removeHandlersFromChannel(IrcChannel channel) {
            channel.UserJoined -= userJoinedChannelEventHandler;
            channel.UserLeft -= userPartedChannelEventHandler;
            channel.MessageReceived -= channelMessageEventHandler;
            channel.NoticeReceived -= channelNoticeEventHandler;
        }

        private void userJoinedChannelEventHandler(object sender, IrcChannelUserEventArgs args)
        {
            var channel = (IrcChannel) sender;
            var chatEvent = new UserEvent() {
                ProviderName = Name,
                EventType = UserEventTypes.Join,
                Channel = channel.Name.ToLowerIrc(),
                User = args.ChannelUser.User.ToChatUserModel(),
            };
            context.PublishChatEvent(chatEvent);
        }

        private void userPartedChannelEventHandler(object sender, IrcChannelUserEventArgs args)
        {
            var channel = (IrcChannel) sender;
            var chatEvent = new UserEvent() {
                ProviderName = Name,
                EventType = UserEventTypes.Part,
                Channel = channel.Name.ToLowerIrc(),
                User = args.ChannelUser.User.ToChatUserModel(),
            };
            context.PublishChatEvent(chatEvent);
        }

        private void channelMessageEventHandler(object sender, IrcMessageEventArgs args)
        {
            publishChannelMessage(sender, args, false);
        }

        private void channelNoticeEventHandler(object sender, IrcMessageEventArgs args)
        {
            publishChannelMessage(sender, args, true);
        }

        private void publishChannelMessage(object sender, IrcMessageEventArgs args, bool isNotice)
        {
            var channel = (IrcChannel) sender;

            var chatMessage = new ChatMessage() {
                ProviderName = Name,
                TextContent = args.Text,
                Channel = channel.Name.ToLowerIrc(),
                IsNotice = isNotice,
            };

            if (args.Source is IrcUser)
            {
                var ircUser = args.Source as IrcUser;
                chatMessage.Sender = ircUser.ToChatUserModel();
            }

            context.PublishChatEvent(chatMessage);
        }
    }
}
