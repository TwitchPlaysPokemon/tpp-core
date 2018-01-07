using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Common
{
    public static class PubSubClientExtensions
    {
        /// <summary>
        /// Serializes the JSON object before calling
        /// <see cref="IPubSubClient.Publish"/>.
        /// </summary>
        public static void Publish(this IPubSubClient client, string topic, JObject message)
        {
            client.Publish(topic, message.ToString());
        }
    }
}
