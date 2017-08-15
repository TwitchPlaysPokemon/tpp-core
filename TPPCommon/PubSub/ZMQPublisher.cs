using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

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
        /// Port number to which the publisher is bound.
        /// </summary>
        private int Port;

        /// <summary>
        /// Maximum number of messages this publisher will queue in memory before potentially dropping messages.
        /// ZMQ's default is 1000.
        /// </summary>
        private const int SendHighWatermark = 1000;

        /// <summary>
        /// Create new instance of ZMQPublisher, and bind to the given port.
        /// </summary>
        public ZMQPublisher()
        {
            this.Socket = InitSocket();
            this.Port = Addresses.PubSubPort;

            string addressToBind = Addresses.BuildFullAddress(Addresses.TCPLocalHost, this.Port);
            this.Socket.Bind(addressToBind);
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
            PublisherSocket socket = new PublisherSocket();
            socket.Options.SendHighWatermark = ZMQPublisher.SendHighWatermark;

            return socket;
        }

        /// <summary>
        /// Publish a message to the given topic.
        /// </summary>
        /// <param name="message">message to publish</param>
        public void PublishMessageString(Topic topic, string message)
        {
            message = message ?? string.Empty;
            string rawTopic = topic.ToString();

            this.Socket.SendMoreFrame(rawTopic);
            this.Socket.SendFrame(message);
        }
    }
}
