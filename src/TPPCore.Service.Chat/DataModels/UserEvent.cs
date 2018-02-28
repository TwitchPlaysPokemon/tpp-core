using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Chat.DataModels
{
    /// <summary>
    /// Represents a change in a user's room membership.
    /// </summary>
    public class UserEvent : ChatEvent
    {
        public string EventType;
        public string Channel;
        public ChatUser User;

        public UserEvent() : base(ChatTopics.UserEvent)
        {
        }

        override public JObject ToJObject()
        {
            var doc = base.ToJObject();
            doc.Add("eventType", EventType);
            doc.Add("channel", Channel);
            doc.Add("user", User.ToJObject());
            return doc;
        }
    }
}
