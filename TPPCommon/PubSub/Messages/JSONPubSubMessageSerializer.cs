using Newtonsoft.Json;

namespace TPPCommon.PubSub.Messages
{
    /// <summary>
    /// JSON implemation of pub-sub message serialization.
    /// </summary>
    public class JSONPubSubMessageSerializer : IPubSubMessageSerializer
    {
        private readonly JsonSerializerSettings DeserializeSettings = new JsonSerializerSettings();
        private readonly JsonSerializerSettings SerializeSettings = new JsonSerializerSettings();
        private Formatting SerializationFormat = Formatting.None;

        /// <summary>
        /// Deserialize the raw pub-sub message into the pub-sub object.
        /// </summary>
        /// <typeparam name="T">pub-sub message type</typeparam>
        /// <param name="rawMessage">raw pub-sub message</param>
        /// <returns>pub-sub message object</returns>
        public T Deserialize<T>(string rawMessage)
        {
            return JsonConvert.DeserializeObject<T>(rawMessage, this.DeserializeSettings);
        }

        /// <summary>
        /// Serialize a strongly-type pub-sub message object into its string repres
        /// </summary>
        /// <param name="message"></param>
        /// <returns>raw message</returns>
        public string Serialize(PubSubMessage message)
        {
            return JsonConvert.SerializeObject(message, this.SerializationFormat, this.SerializeSettings);
        }

        /// <summary>
        /// Sets whether or not serialization will be human-friendly.
        /// </summary>
        /// <param name="prettyFormatting"></param>
        public void SetPrettySerializationFormatting(bool prettyFormatting)
        {
            this.SerializationFormat = prettyFormatting ? Formatting.Indented : Formatting.None;
        }
    }
}
