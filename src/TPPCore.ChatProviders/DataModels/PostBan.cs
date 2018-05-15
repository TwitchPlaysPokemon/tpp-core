namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// A request to ban a user from a channel.
    /// </summary>
    public class PostBan : PostObject
    {
        /// <summary>
        /// The channel to ban them from.
        /// </summary>
        public string Channel;

        /// <summary>
        /// The user to ban.
        /// </summary>
        public ChatUser User;

        /// <summary>
        /// The reason for the ban.
        /// </summary>
        public string Reason;
    }
}
