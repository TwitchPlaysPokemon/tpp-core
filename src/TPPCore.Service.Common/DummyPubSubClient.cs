using log4net;
using System.Collections.Generic;

namespace TPPCore.Service.Common
{
    public class DummyPubSubClientMessage
    {
        public readonly string Topic;
        public readonly string Message;

        public DummyPubSubClientMessage(string topic, string message)
        {
            Topic = topic;
            Message = message;
        }
    }

    public class DummyPubSubClient : IPubSubClient
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public List<DummyPubSubClientMessage> Messages;

        public DummyPubSubClient()
        {
            Messages = new List<DummyPubSubClientMessage>();
        }

        public void Publish(string topic, string message)
        {
            logger.DebugFormat("Publish: {0} {1}", topic, message);
            Messages.Add(new DummyPubSubClientMessage(topic, message));
        }
    }
}
