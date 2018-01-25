using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Chat.DataModels
{
    public class ChatEvent : IPubSubEvent
    {
        public string Topic { get; set; } = ChatTopics.Other;
        public string ProviderName;

        public virtual JObject ToJObject() {
            return JObject.FromObject(new
            {
                topic = Topic,
                providerName = ProviderName,
            });
        }
    }
}
