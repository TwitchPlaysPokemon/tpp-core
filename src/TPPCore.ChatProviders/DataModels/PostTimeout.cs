namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// A request to time a user out for a specified amount of time.
    /// </summary>
    public class PostTimeout : PostObject
    {
        /// <summary>
        /// The channel to time them out from.
        /// </summary>
        public string Channel;

        /// <summary>
        /// The user to time out.
        /// </summary>
        public ChatUser User;

        /// <summary>
        /// The reason for the timeout.
        /// </summary>
        public string Reason;

        /// <summary>
        /// How long to time the user out for.
        /// </summary>
        public int Duration;
    }
}
