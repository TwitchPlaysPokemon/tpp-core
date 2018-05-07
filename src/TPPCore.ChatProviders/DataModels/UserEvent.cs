namespace TPPCore.ChatProviders.DataModels
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
    }
}
