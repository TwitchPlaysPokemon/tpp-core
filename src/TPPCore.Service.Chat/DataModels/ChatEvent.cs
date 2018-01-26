using System.Diagnostics;
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
            Debug.Assert(Topic != null);
            Debug.Assert(ProviderName != null);

            return JObject.FromObject(new
            {
                topic = Topic,
                providerName = ProviderName,
            });
        }
    }
}
