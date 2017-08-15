namespace TPPCommon.PubSub
{
    /// <summary>
    /// Interface for a publisher in the pub-sub pattern.
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// Publish a string message to the given topic.
        /// </summary>
        /// <param name="topic">pub-sub topic</param>
        /// <param name="message">message</param>
        void PublishMessageString(Topic topic, string message);
    }
}
