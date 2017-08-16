using TPPCommon.PubSub.Messages;

namespace TPPCommon.PubSub
{
    /// <summary>
    /// Interface for a publisher in the pub-sub pattern.
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// Publish a pub-sub message.
        /// </summary>
        /// <param name="message">message</param>
        void Publish(PubSubMessage message);
    }
}
