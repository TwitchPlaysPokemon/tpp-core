using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Chat.DataModels
{
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
}
