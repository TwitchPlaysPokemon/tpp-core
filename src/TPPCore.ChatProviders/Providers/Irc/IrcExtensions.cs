using TPPCore.Irc;
using TPPCore.ChatProviders.DataModels;

namespace TPPCore.ChatProviders.Irc
{
    public static class IrcExtensions
    {
        public static ChatUser ToChatUserModel(this ClientId clientId)
        {
            return new ChatUser() {
                UserId = clientId.ToString(),
                Nickname = clientId.Nickname,
                Username = clientId.NicknameLower,
                Host = clientId.Host
            };
        }
    }
}
