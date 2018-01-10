using log4net;
using System;
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

    /// <summary>
    /// Pub/sub client that only works in the same process and stores all
    /// the messages.
    /// </summary>
    public class DummyPubSubClient : IPubSubClient
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public List<DummyPubSubClientMessage> Messages;
        private List<(string Topic, Action<string,string> Handler)> handlers;

        public DummyPubSubClient()
        {
            Messages = new List<DummyPubSubClientMessage>();
            handlers = new List<(string topic, Action<string,string> handler)>();
        }

        public void Publish(string topic, string message)
        {
            logger.DebugFormat("Publish: {0} {1}", topic, message);
            Messages.Add(new DummyPubSubClientMessage(topic, message));

            foreach (var item in handlers)
            {
                if (item.Topic.Equals(topic, StringComparison.Ordinal))
                {
                    item.Handler(topic, message);
                }
            }
        }

        public void Subscribe(string topic, Action<string, string> handler)
        {
            logger.DebugFormat("Subscribe: {0} {1}", topic, handler);

            var handlerItem = (Topic: topic, Handler: handler);
            if (!handlers.Contains(handlerItem))
            {
                handlers.Add(handlerItem);
            }
        }

        public void Unsubscribe(string topic, Action<string, string> handler)
        {
            var handlerItem = (Topic: topic, Handler: handler);
            handlers.Remove(handlerItem);
        }
    }
}
