namespace TPPCommon.PubSub.Events
{
    /// <summary>
    /// Interface for handling pub-sub events serializationg and deserialization.
    /// </summary>
    public interface IPubSubEventSerializer
    {
        /// <summary>
        /// Create a strongly-typed pub-sub event object based on the raw message.
        /// </summary>
        /// <typeparam name="T">pub-sub event type</typeparam>
        /// <param name="rawEvent">raw pub-sub message</param>
        /// <returns>pub-sub event object</returns>
        T Deserialize<T>(string rawEvent);

        /// <summary>
        /// Serialize a strongly-type pub-sub event object into its string representation.
        /// </summary>
        /// <param name="event"></param>
        /// <returns>raw message</returns>
        string Serialize(PubSubEvent @event);

        /// <summary>
        /// Sets whether or not serialization will be human-friendly.
        /// </summary>
        /// <param name="prettyFormatting"></param>
        void SetPrettySerializationFormatting(bool prettyFormatting);
    }
}
