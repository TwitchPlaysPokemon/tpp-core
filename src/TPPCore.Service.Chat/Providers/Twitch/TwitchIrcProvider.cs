using System.Threading.Tasks;
using TPPCore.Service.Chat.Providers.Irc;
using TPPCore.Irc;

namespace TPPCore.Service.Chat.Twitch
{
    public class TwitchIrcProvider : IrcProvider
    {
        public TwitchIrcProvider()
        {
            ProviderName = "twitch";
        }

        override protected async Task login()
        {
            await base.login();
            await ircClient.SendMessage("CAP", "REQ", null,
                "twitch.tv/membership twitch.tv/tags twitch.tv/commands");
        }

        override protected void setUpEventHandlers()
        {
            base.setUpEventHandlers();
            ircClient.CommandHandlers.AddOrCombine("USERNOTICE", userNoticeEventHandler);
            ircClient.CommandHandlers.AddOrCombine("WHISPER", whisperEventHandler);
        }

        private Task userNoticeEventHandler(IrcClient client, Message message)
        {
            // TODO: publish this event
            return Task.CompletedTask;
        }

        private Task whisperEventHandler(IrcClient client, Message message)
        {
            // TODO: publish this event
            return Task.CompletedTask;
        }
    }
}
