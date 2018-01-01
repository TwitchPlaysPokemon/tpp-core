using Newtonsoft.Json;

namespace TPPCommon.PubSub.Events
{
    /// <summary>
    /// JSON implemation of pub-sub event serialization.
    /// </summary>
    public class JSONPubSubEventSerializer : IPubSubEventSerializer
    {
        private readonly JsonSerializerSettings DeserializeSettings = new JsonSerializerSettings();
        private readonly JsonSerializerSettings SerializeSettings = new JsonSerializerSettings();
        private Formatting SerializationFormat = Formatting.None;

        /// <summary>
        /// Deserialize the raw pub-sub event into the pub-sub object.
        /// </summary>
        /// <typeparam name="T">pub-sub event type</typeparam>
        /// <param name="rawEvent">raw pub-sub event</param>
        /// <returns>pub-sub event object</returns>
        public T Deserialize<T>(string rawEvent)
        {
            return JsonConvert.DeserializeObject<T>(rawEvent, this.DeserializeSettings);
        }

        /// <summary>
        /// Serialize a strongly-type pub-sub event object into its string repres
        /// </summary>
        /// <param name="event"></param>
        /// <returns>raw event</returns>
        public string Serialize(PubSubEvent @event)
        {
            return JsonConvert.SerializeObject(@event, this.SerializationFormat, this.SerializeSettings);
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
