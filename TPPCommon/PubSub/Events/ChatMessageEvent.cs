using System.Runtime.Serialization;
using TPPCommon.Chat.Client;

namespace TPPCommon.PubSub.Events
{
    /// <summary>
    /// Event for a chat message received from a chat service.
    /// </summary>
    [DataContract]
    [Topic("chat")]
    public class ChatMessageEvent : PubSubEvent
    {
        [DataMember]
        public string ServiceName { get; set; }

        [DataMember]
        public ChatMessage Message { get; set; }

        public ChatMessageEvent(string serviceName, ChatMessage message) {
            ServiceName = serviceName;
            Message = message;
        }
    }
}
