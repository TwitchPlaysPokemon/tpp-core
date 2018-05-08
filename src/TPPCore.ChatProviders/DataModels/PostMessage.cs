namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// A message to send to chat.
    /// </summary>
    public class PostMessage : PostObject
    {
        /// <summary>
        /// The channel to post the message to.
        /// </summary>
        public string Channel;

        /// <summary>
        /// Message to send.
        /// </summary>
        public string Message;
    }
}
