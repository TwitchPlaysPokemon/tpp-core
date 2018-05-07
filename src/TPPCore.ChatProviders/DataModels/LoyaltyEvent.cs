namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// Represents user initiated events such as subscription, hosting, etc.
    /// </summary>
    public class LoyaltyEvent : ChatEvent
    {
        public LoyaltyEvent() : base(ChatTopics.Loyalty) {
        }

        /// <summary>
        /// Who sent this message.
        /// </summary>
        public ChatUser Sender;

        /// <summary>
        /// Message contents such as a greeting or a textual representation of the event.
        /// </summary>
        /// <remarks>
        /// This value may be null.
        /// </remarks>
        public string TextContent;

        /// <summary>
        /// Chat room where event is occuring.
        /// </summary>
        public string Channel;
    }
}
