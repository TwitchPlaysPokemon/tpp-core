using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Chat.DataModels
{
    public class ChatMessage : ChatEvent
    {
        public ChatUser Sender;
        public string TextContent;
        public string Channel;
        public bool IsNotice = false;

        public ChatMessage() : base(ChatTopics.Message)
        {
        }

        override public JObject ToJObject()
        {
            var doc = base.ToJObject();
            doc.Add("sender", Sender != null ? Sender.ToJObject() : null);
            doc.Add("textContent", TextContent);
            doc.Add("channel", Channel);
            doc.Add("isNotice", IsNotice);

            return doc;
        }
    }
}
