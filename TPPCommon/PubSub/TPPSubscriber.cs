using TPPCommon.PubSub.Messages;

namespace TPPCommon.PubSub
{
    /// <summary>
    /// Class that manages subscribing to TPP PubSub messaging layer.
    /// </summary>
    public class TPPSubscriber
    {
        private ISubscriber Subscriber;

        public TPPSubscriber(ISubscriber subscriber)
        {
            this.Subscriber = subscriber;
        }

        /// <summary>
        /// Subscribe to a type of pubsub messages.
        /// </summary>
        /// <param name="handler">message handler</param>
        public void Subscribe<T>(PubSubMessageHandler<T> handler) where T : PubSubMessage
        {
            this.Subscriber.Subscribe(handler);
        }
    }
}
