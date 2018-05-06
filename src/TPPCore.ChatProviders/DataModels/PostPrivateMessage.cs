using Newtonsoft.Json.Linq;

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

        override public JObject ToJObject()
        {
            var doc = base.ToJObject();
            doc.Add("user", User);
            doc.Add("message", Message);

            return doc;
        }
    }
}
