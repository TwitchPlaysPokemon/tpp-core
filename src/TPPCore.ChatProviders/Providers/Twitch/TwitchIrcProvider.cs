using System.Threading.Tasks;
using TPPCore.ChatProviders.Providers.Irc;
using TPPCore.Irc;
using TPPCore.ChatProviders.DataModels;
using System.Linq;

namespace TPPCore.ChatProviders.Twitch
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

        override protected ChatUser getMessageSender(Message message)
        {
            return new ChatUser() {
                UserId = message.Tags.ContainsKey("user-id")
                    ? message.Tags["user-id"] : null,
                Username = message.Tags.ContainsKey("login")
                    ? message.Tags["login"]
                    : (message.Prefix.ClientId != null
                        ? message.Prefix.ClientId.Nickname : null),
                Nickname = message.Tags.ContainsKey("display-name")
                    ? message.Tags["display-name"] : null
            };
        }

        private Task userNoticeEventHandler(IrcClient client, Message message)
        {
            var chatEvent = new LoyaltyEvent() {
                ClientName = ClientName,
                ProviderName = ProviderName,
                TextContent = message.TrailingParameter,
                Channel = message.TargetLower,
                Sender = getMessageSender(message),
                IsSelf = isMessageSelf(message)
            };
            message.Tags.ToList().ForEach(item => chatEvent.Meta.Add(item));

            context.PublishChatEvent(chatEvent);
            return Task.CompletedTask;
        }

        private Task whisperEventHandler(IrcClient client, Message message)
        {
            var chatMessage = new ChatMessage() {
                ClientName = ClientName,
                ProviderName = ProviderName,
                TextContent = message.TrailingParameter,
                Sender = getMessageSender(message),
                IsSelf = isMessageSelf(message)
            };
            message.Tags.ToList().ForEach(item => chatMessage.Meta.Add(item));

            context.PublishChatEvent(chatMessage);
            return Task.CompletedTask;
        }
    }
}
