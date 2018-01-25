using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Chat.DataModels
{
    public class ChatMessage : ChatEvent
    {
        public new string Topic { get; set; } = ChatTopics.Message;

        public ChatUser Sender;
        public string TextContent;
        public string Channel;
        public bool IsSelf = false;

        override public JObject ToJObject()
        {
            var doc = base.ToJObject();
            doc.Add("sender", Sender != null ? Sender.ToJObject() : null);
            doc.Add("textContent", TextContent);
            doc.Add("channel", Channel);
            doc.Add("isSelf", IsSelf);

            return doc;
        }
    }
}
