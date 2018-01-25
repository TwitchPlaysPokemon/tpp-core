using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Chat.DataModels
{
    public class ChatEvent : IPubSubEvent
    {
        public string Topic { get; set; }
        public string ProviderName;

        public ChatEvent(string topic = ChatTopics.Other)
        {
            this.Topic = topic;
        }

        public virtual JObject ToJObject()
        {
            return JObject.FromObject(new
            {
                topic = Topic,
                providerName = ProviderName,
            });
        }
    }
}
