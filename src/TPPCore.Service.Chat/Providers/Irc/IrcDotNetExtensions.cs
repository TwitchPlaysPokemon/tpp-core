using IrcDotNet;
using TPPCore.Service.Chat.DataModels;
using TPPCore.Utils;

namespace TPPCore.Service.Chat.Irc
{
    public static class IrcDotNetExtensions
    {
        public static ChatUser ToChatUserModel(this IrcUser ircUser)
        {
            return new ChatUser() {
                UserId = $"{ircUser.NickName}!{ircUser.UserName}@{ircUser.HostName}",
                Nickname = ircUser.NickName,
                Username = ircUser.NickName.ToLowerIrc()
            };
        }
    }
}
