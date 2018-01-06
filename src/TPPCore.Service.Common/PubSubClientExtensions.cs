using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Common
{
    public static class PubSubClientExtensions
    {
        public static void Publish(this IPubSubClient client, string topic, JObject message)
        {
            client.Publish(topic, message.ToString());
        }
    }
}
