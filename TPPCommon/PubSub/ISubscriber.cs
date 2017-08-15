namespace TPPCommon.PubSub
{
    public delegate void PubSubMessageHandler(PubSubMessage message);

    /// <summary>
    /// Interface for a subscriber in the pub-sub pattern.
    /// </summary>
    public interface ISubscriber
    {
        /// <summary>
        /// Subscribe to the given topic with a handler function.
        /// </summary>
        /// <param name="topic">pub-sub topic</param>
        void Subscribe(Topic topic, PubSubMessageHandler handler);
    }
}
