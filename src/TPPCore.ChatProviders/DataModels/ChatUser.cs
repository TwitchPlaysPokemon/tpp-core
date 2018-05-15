using System.Collections.Generic;

namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// Identification of a chat user.
    /// </summary>
    public class ChatUser
    {
        /// <summary>
        /// The User ID of the user.
        /// </summary>
        public string UserId;

        /// <summary>
        /// The Username of the user.
        /// </summary>
        public string Username;

        /// <summary>
        /// The nickname of the user.
        /// </summary>
        public string Nickname;

        /// <summary>
        /// The host name or IP address of the user.
        /// </summary>
        /// <remarks>
        /// This is only user for IRC so far.
        /// </remarks>
        public string Host;

        /// <summary>
        /// The Access level of the user.
        /// </summary>
        public AccessLevel AccessLevel;

    }

    public class ChatUserEqualityComparer : EqualityComparer<ChatUser>
    {
        public override bool Equals(ChatUser x, ChatUser y)
        {
            if (x.UserId != null && y.UserId != null)
            {
                return x.UserId.Equals(y.UserId);
            }

            return x.Username == y.Username;
        }

        public override int GetHashCode(ChatUser obj)
        {
            if (obj.UserId != null)
            {
                return obj.UserId.GetHashCode();
            }

            return obj.Username.GetHashCode();
        }
    }
}
