namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// A message to send directly to a user.
    /// </summary>
    public class PostPrivateMessage : PostObject
    {
        /// <summary>
        /// The user to send the message to.
        /// </summary>
        public string User;

        /// <summary>
        /// Message to send.
        /// </summary>
        public string Message;
    }
}
