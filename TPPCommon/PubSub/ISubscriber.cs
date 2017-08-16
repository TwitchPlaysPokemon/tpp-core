using TPPCommon.PubSub.Events;

namespace TPPCommon.PubSub
{
    /// <summary>
    /// Delegate function definition for received event processing.
    /// </summary>
    /// <param name="event">pub-sub event object</param>
    public delegate void PubSubEventHandler<T>(T @event) where T : PubSubEvent;

    /// <summary>
    /// Interface for a subscriber in the pub-sub pattern.
    /// </summary>
    public interface ISubscriber
    {
        /// <summary>
        /// Subscribe to the given topic with a handler function.
        /// </summary>
        /// <param name="handler">pub-sub event handler delegate function</param>
        void Subscribe<T>(PubSubEventHandler<T> handler) where T : PubSubEvent;
    }
}
