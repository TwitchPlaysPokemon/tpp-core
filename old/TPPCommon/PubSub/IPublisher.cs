using TPPCommon.PubSub.Events;

namespace TPPCommon.PubSub
{
    /// <summary>
    /// Interface for a publisher in the pub-sub pattern.
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// Publish a pub-sub event.
        /// </summary>
        /// <param name="event">event</param>
        void Publish(PubSubEvent @event);
    }
}
