using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

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
        private Dictionary<Topic, PubSubMessageHandler> MessageHandlers = new Dictionary<Topic, PubSubMessageHandler>();

        /// <summary>
        /// Create new instance of ZMQSubscriber, and connect to the given port.
        /// </summary>
        public ZMQSubscriber()
        {
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
            string topic = args.Socket.ReceiveFrameString();
            string rawMessage = args.Socket.ReceiveFrameString();
            PubSubMessage message = new PubSubMessage(topic, rawMessage);

            // Invoke the designated handler function on the received message.
            if (MessageHandlers.ContainsKey(message.Topic))
            {
                PubSubMessageHandler handler = MessageHandlers[message.Topic];
                handler(message);
            }
        }

        /// <summary>
        /// Subscribe to the given topic with a handler function.
        /// </summary>
        /// <param name="topic">pub-sub topic</param>
        public void Subscribe(Topic topic, PubSubMessageHandler handler)
        {
            MessageHandlers.Add(topic, handler);

            string topicString = topic.ToString();
            this.Socket.Subscribe(topicString);
        }
    }
}
