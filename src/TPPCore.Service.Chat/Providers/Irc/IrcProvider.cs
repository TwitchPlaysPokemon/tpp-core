using IrcDotNet;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TPPCore.Utils;
using System;
using TPPCore.Service.Chat.DataModels;

namespace TPPCore.Service.Chat.Providers.Irc
{
    public class IrcProvider : IProviderThreaded
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Name { get { return "irc"; } }

        private Object thisLock = new Object();
        private ProviderContext context;
        private StandardIrcClient ircClient;
        private ConnectConfig connectConfig;
        private bool isRunning = false;


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
                Nickname = context.Service.ConfigReader.GetCheckedValue<string>(
                    new[] {"chat", Name, "nickname"}),
                Password = context.Service.ConfigReader.GetCheckedValueOrDefault<string>(
                    new[] {"chat", Name, "nickname"}, null),
                Channels = context.Service.ConfigReader.GetCheckedValue<string[]>(
                    new[] {"chat", Name, "channels"}),
            };
        }

        public void Run()
        {
            isRunning = true;

            while (isRunning)
            {
                initIrcClient();

                var disconnectEvent = connectAndWaitLogin();

                if (ircClient.IsRegistered)
                {
                    logger.Info("Logged in.");
                    setUpEvents();
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
        }

        private void initIrcClient()
        {
            ircClient = new StandardIrcClient();
            // TODO: Allow this to be customized
            ircClient.FloodPreventer = new IrcStandardFloodPreventer(20, 30000);

            ircClient.RawMessageReceived +=
                (object sender, IrcRawMessageEventArgs args) =>
                    logger.DebugFormat("IRC RECV: {0}", args.RawContent);

            ircClient.RawMessageSent +=
                (object sender, IrcRawMessageEventArgs args) =>
                    logger.DebugFormat("IRC SEND: {0}", args.RawContent);
        }

        private void cleanUpIrcClient()
        {
            lock (thisLock)
            {
                if (ircClient == null)
                {
                    return;
                }

                try {
                    if (ircClient.IsConnected)
                    {
                        ircClient.Quit(5, "");
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

                    // TODO: allow configuring timeout
                    connectEvent.Wait(120000);
                }

                if (!ircClient.IsConnected)
                {
                    logger.Error("Connect timed out.");
                    ircClient.Disconnect();
                    return disconnectEvent;
                }

                logger.Info("Connected. Logging in...");
                var success = registerEvent.Wait(120000);

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

        private void setUpEvents()
        {
            // TODO: this is a good place to set up the delegates
            // Add handlers to broadcast chat messages received.
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
                var user = new ChatUser() {
                    UserId = $"{ircUser.User.NickName}!{ircUser.User.UserName}@{ircUser.User.ServerName}",
                    Nickname = ircUser.User.NickName,
                    Username = ircUser.User.NickName.ToLowerIrc()
                };
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
    }
}
