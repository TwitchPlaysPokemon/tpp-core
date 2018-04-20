using Newtonsoft.Json.Linq;

namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// A user's chat message.
    /// </summary>
    public class ChatMessage : ChatEvent
    {
        /// <summary>
        /// Who sent this message.
        /// </summary>
        public ChatUser Sender;

        /// <summary>
        /// Message contents.
        /// </summary>
        public string TextContent;

        /// <summary>
        /// Originating chat room.
        /// </summary>
        /// <remarks>
        /// This value will be null if the message did not originate in a room,
        /// ie, a private message or whisper.
        /// </remarks>
        public string Channel;

        /// <summary>
        /// Whether the message is an IRC-style notice message.
        /// </summary>
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
