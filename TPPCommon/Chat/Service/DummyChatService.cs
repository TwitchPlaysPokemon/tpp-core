using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TPPCommon.Chat;
using TPPCommon.Configuration;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TPPCommon.Chat.Service
{
    public class DummyChatService : BaseChatService
    {
        protected override string ServiceName
        {
            get => "dummy_chat";
        }

        public DummyChatService(
                IPublisher publisher,
                ISubscriber subscriber,
                ITPPLoggerFactory loggerFactory,
                IConfigReader configReader) :
                base (publisher, subscriber, loggerFactory, configReader)
        {
            ChatClient = new Client.DummyChatClient();
            ConnectionConfig = new Client.ConnectionConfig();
        }
    }
}
