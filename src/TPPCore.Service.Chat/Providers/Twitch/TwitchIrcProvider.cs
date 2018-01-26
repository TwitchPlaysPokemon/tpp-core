using IrcDotNet;
using TPPCore.Service.Chat.Providers.Irc;

namespace TPPCore.Service.Chat.Twitch
{
    public class TwitchIrcProvider : IrcProvider
    {
        public TwitchIrcProvider()
        {
            Name = "twitch";
        }

        override protected StandardIrcClient newIrcClient()
        {
            return new TwitchIrcClient();
        }
    }
}
