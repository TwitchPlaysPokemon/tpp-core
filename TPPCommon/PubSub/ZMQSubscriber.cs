using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using TPPCommon.PubSub.Events;

namespace TPPCommon.PubSub
{
    public class ZMQSubscriber : ISubscriber
    {
        /// <summary>
        /// Subscriber socket context.
        /// </summary>
        private SubscriberSocket Socket;

        /// <summary>
        /// Poller to process incoming messages.
        /// </summary>
        private NetMQPoller Poller;

        /// <summary>
        /// Port number to which the subscriber is connected.
        /// </summary>
        private int Port;

        /// <summary>
        /// Maximum number of messages this subscriber will queue in memory before potentially dropping messages.
        /// ZMQ's default is 1000.
        /// </summary>
        private const int ReceiveHighWatermark = 1000;

        /// <summary>
        /// Mapping for pub-sub topics and functions that will process them as they are received.
        /// </summary>
        private Dictionary<Topic, MessageHandler> MessageHandlers = new Dictionary<Topic, MessageHandler>();

        /// <summary>
        /// Serializer object used for transforming messages.
        /// </summary>
        private IPubSubEventSerializer Serializer;

        /// <summary>
        /// Create new instance of ZMQSubscriber, and connect to the given port.
        /// </summary>
        public ZMQSubscriber(IPubSubEventSerializer serializer)
        {
            this.Serializer = serializer;

            this.Socket = InitSocket();
            this.Port = Addresses.PubSubPort;

            // Connect socket to publisher.
            string addressToConnect = Addresses.BuildFullAddress(Addresses.TCPLocalHost, this.Port);
            this.Socket.Connect(addressToConnect);

            // Setup non-blocking polling for subscriber socket.
            this.Poller = new NetMQPoller();
            this.Poller.Add(this.Socket);
            this.Socket.ReceiveReady += OnReceiveReady;
            this.Poller.RunAsync();
        }

        ~ZMQSubscriber()
        {
            // Clean up publisher socket context.
            if (this.Socket != null)
            {
                this.Socket.Dispose();
            }

            if (this.Poller != null)
            {
                this.Poller.StopAsync();
                this.Poller.Dispose();
            }
        }

        private SubscriberSocket InitSocket()
        {
            SubscriberSocket socket = new SubscriberSocket();
            socket.Options.SendHighWatermark = ZMQSubscriber.ReceiveHighWatermark;

            return socket;
        }

        /// <summary>
        /// Entrypoint for handling incoming published messages.
        /// </summary>
        private void OnReceiveReady(object sender, NetMQSocketEventArgs args)
        {
            string rawTopic = args.Socket.ReceiveFrameString();
            string rawMessage = args.Socket.ReceiveFrameString();

            Topic topic;
            if (!Enum.TryParse(rawTopic, out topic))
            {
                throw new InvalidTopicException($"Invalid pub-sub topic was received: '{rawTopic}'", nameof(rawTopic));
            }

            // Invoke the designated handler function on the received message.
            if (MessageHandlers.ContainsKey(topic))
            {
                var handler = MessageHandlers[topic];
                handler.ProcessMessage(rawMessage);
            }
        }

        /// <summary>
        /// Subscribe to the given topic with a handler function.
        /// </summary>
        /// <param name="topic">pub-sub topic</param>
        public void Subscribe<T>(PubSubEventHandler<T> handler) where T : PubSubEvent
        {
            Topic topic = PubSubEvent.GetTopicForEventType(typeof(T));
            MessageHandlers.Add(topic, new MessageHandler<T>(handler, this.Serializer));

            string topicString = topic.ToString();
            this.Socket.Subscribe(topicString);
        }

        /// <summary>
        /// Helper classes to allow generic assignment of message topic handlers.
        /// These allow compile-time enforcement of message handler function types when subscribing to a topic.
        /// </summary>
        private abstract class MessageHandler
        {
            /// <summary>
            /// Deserialize and call the handler function for the incoming message.
            /// </summary>
            /// <param name="rawMessage">raw pub-sub message</param>
            public abstract void ProcessMessage(string rawMessage);
        }

        private class MessageHandler<T> : MessageHandler where T : PubSubEvent
        {
            private PubSubEventHandler<T> Handler;
            private IPubSubEventSerializer Serializer;

            public MessageHandler(PubSubEventHandler<T> handler, IPubSubEventSerializer serializer)
            {
                this.Handler = handler;
                this.Serializer = serializer;
            }

            /// <summary>
            /// Deserialize and call the handler function for the incoming message.
            /// </summary>
            /// <param name="rawMessage">raw pub-sub message</param>
            public override void ProcessMessage(string rawMessage)
            {
                T @event = this.Serializer.Deserialize<T>(rawMessage);
                this.Handler(@event);
            }
        }
    }
}
