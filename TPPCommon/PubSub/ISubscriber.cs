using TPPCommon.PubSub.Messages;

namespace TPPCommon.PubSub
{
    /// <summary>
    /// Delegate function definition for received message processing.
    /// </summary>
    /// <param name="message">pub-sub message object</param>
    public delegate void PubSubMessageHandler<T>(T message) where T : PubSubMessage;

    /// <summary>
    /// Interface for a subscriber in the pub-sub pattern.
    /// </summary>
    public interface ISubscriber
    {
        /// <summary>
        /// Subscribe to the given topic with a handler function.
        /// </summary>
        /// <param name="topic">pub-sub topic</param>
        /// <param name="handler">pub-sub message handler delegate function</param>
        void Subscribe<T>(Topic topic, PubSubMessageHandler<T> handler) where T : PubSubMessage;
    }
}
