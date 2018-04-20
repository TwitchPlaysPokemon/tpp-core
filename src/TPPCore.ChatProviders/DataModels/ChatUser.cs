using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// Identification of a chat user.
    /// </summary>
    public class ChatUser
    {
        public string UserId;
        public string Username;
        public string Nickname;

        public JObject ToJObject()
        {
            return JObject.FromObject(new
            {
                userId = UserId,
                username = Username,
                nickname = Nickname
            });
        }
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
