using TPPCore.Irc;
using TPPCore.Service.Chat.DataModels;
using TPPCore.Utils;

namespace TPPCore.Service.Chat.Irc
{
    public static class IrcExtensions
    {
        public static ChatUser ToChatUserModel(this ClientId clientId)
        {
            return new ChatUser() {
                UserId = clientId.ToString(),
                Nickname = clientId.Nickname,
                Username = clientId.NicknameLower
            };
        }
    }
}
