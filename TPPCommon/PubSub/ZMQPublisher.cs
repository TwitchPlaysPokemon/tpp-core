using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using TPPCommon.PubSub.Events;

namespace TPPCommon.PubSub
{
    /// <summary>
    /// ZMQ implementation of Publisher in pub-sub pattern.
    /// </summary>
    public class ZMQPublisher : IPublisher
    {
        /// <summary>
        /// Publisher Socket context.
        /// </summary>
        private PublisherSocket Socket;

        /// <summary>
        /// Maximum number of events this publisher will queue in memory before potentially dropping events.
        /// ZMQ's default is 1000.
        /// </summary>
        private const int SendHighWatermark = 1000;

        /// <summary>
        /// Serializer object used for transforming events into raw messages.
        /// </summary>
        private IPubSubEventSerializer Serializer;

        /// <summary>
        /// Create new instance of ZMQPublisher, and bind to the given port.
        /// </summary>
        public ZMQPublisher(IPubSubEventSerializer serializer)
        {
            this.Serializer = serializer;
            this.Socket = InitSocket();
        }

        ~ZMQPublisher()
        {
            // Clean up publisher socket context.
            if (this.Socket != null)
            {
                this.Socket.Dispose();
            }
        }

        private PublisherSocket InitSocket()
        {
            string addressToBind = ">" + Addresses.BuildFullAddress(Addresses.TCPLocalHost, Addresses.SubscriberPort);
            PublisherSocket socket = new PublisherSocket(addressToBind);
            socket.Options.SendHighWatermark = ZMQPublisher.SendHighWatermark;

            return socket;
        }

        /// <summary>
        /// Publish an event to the given topic.
        /// </summary>
        /// <param name="event">event to publish</param>
        public void Publish(PubSubEvent @event)
        {
            string rawTopic = @event.GetTopic();
            string rawMessage = this.Serializer.Serialize(@event);

            this.Socket.SendMoreFrame(rawTopic);
            this.Socket.SendFrame(rawMessage);
        }
    }
}
