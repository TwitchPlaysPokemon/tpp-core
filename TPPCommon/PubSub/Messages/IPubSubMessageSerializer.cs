namespace TPPCommon.PubSub.Messages
{
    /// <summary>
    /// Interface for handling pub-sub message serializationg and deserialization.
    /// </summary>
    public interface IPubSubMessageSerializer
    {
        /// <summary>
        /// Create a strongly-typed pub-sub message object based on the raw message.
        /// </summary>
        /// <typeparam name="T">pub-sub message type</typeparam>
        /// <param name="rawMessage">raw pub-sub message</param>
        /// <returns>pub-sub message object</returns>
        T Deserialize<T>(string rawMessage);

        /// <summary>
        /// Serialize a strongly-type pub-sub message object into its string representation.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>raw message</returns>
        string Serialize(PubSubMessage message);

        /// <summary>
        /// Sets whether or not serialization will be human-friendly.
        /// </summary>
        /// <param name="prettyFormatting"></param>
        void SetPrettySerializationFormatting(bool prettyFormatting);
    }
}
