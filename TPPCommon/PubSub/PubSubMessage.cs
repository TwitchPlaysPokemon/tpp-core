namespace TPPCommon.PubSub
{
    /// <summary>
    /// Container to represent a pub-sub message.
    /// </summary>
    public class PubSubMessage
    {
        /// <summary>
        /// The topic for the pub-sub message.
        /// </summary>
        public Topic Topic { get; set; }

        /// <summary>
        /// The message content.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Instantiate a new PubSubMessage.
        /// </summary>
        /// <param name="topic">pub-sub topic</param>
        /// <param name="message">message</param>
        public PubSubMessage(Topic topic, string message)
        {
            this.Topic = topic;
            this.Message = message ?? string.Empty;
        }

        /// <summary>
        /// Instantiate a new PubSubMessage.
        /// </summary>
        /// <param name="topic">pub-sub topic</param>
        /// <param name="message">message</param>
        public PubSubMessage(string topic, string message)
        {
            Topic parsedTopic;
            if (!Topic.TryParse(topic, out parsedTopic))
            {
                throw new InvalidTopicException($"Failed to parse topic '{topic}'.", nameof(topic));
            }

            this.Topic = parsedTopic;
            this.Message = message ?? string.Empty;
        }
    }
}
