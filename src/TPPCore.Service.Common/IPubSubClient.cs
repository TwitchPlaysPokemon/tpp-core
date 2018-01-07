using System;

namespace TPPCore.Service.Common
{
    /// <summary>
    /// Client for communicating to a pub/sub server.
    /// </summary>
    public interface IPubSubClient
    {
        /// <summary>
        /// Broadcasts a message under the given topic.
        /// </summary>
        void Publish(string topic, string message);

        /// <summary>
        /// Listens to messages matching the given topic.
        /// </summary>
        void Subscribe(string topic, Action<string,string> handler);

        /// <summary>
        /// Stops listening to messages matching the given topic.
        /// </summary>
        void Unsubscribe(string topic, Action<string,string> handler);
    }
}
