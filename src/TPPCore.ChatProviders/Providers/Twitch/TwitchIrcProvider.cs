using System.Threading.Tasks;
using TPPCore.ChatProviders.Providers.Irc;
using TPPCore.Irc;
using TPPCore.ChatProviders.DataModels;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Diagnostics;

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
            await ircClient.SendParamsTrailing("CAP", "REQ",
                "twitch.tv/membership twitch.tv/tags twitch.tv/commands");
        }

        override protected void setUpEventHandlers()
        {
            base.setUpEventHandlers();
            ircClient.CommandHandlers.Remove("PRIVMSG");
            ircClient.CommandHandlers.AddOrCombine("PRIVMSG", MessageEventHandler);
            ircClient.CommandHandlers.AddOrCombine("USERNOTICE", UserNoticeEventHandler);
            ircClient.CommandHandlers.AddOrCombine("WHISPER", WhisperEventHandler);
        }

        override protected ChatUser getMessageSender(Message message)
        {
            return new ChatUser()
            {
                UserId = message.Tags.ContainsKey("user-id")
                    ? message.Tags["user-id"] : null,
                Username = message.Tags.ContainsKey("login")
                    ? message.Tags["login"]
                    : (message.Prefix.ClientId?.Nickname),
                Nickname = message.Tags.ContainsKey("display-name")
                    ? message.Tags["display-name"] : null
            };
        }

        private Task MessageEventHandler(IrcClient client, Message message)
        {
            var chatMessage = new ChatMessage()
            {
                ClientName = ClientName,
                ProviderName = ProviderName,
                TextContent = message.TrailingParameter,
                Channel = message.TargetLower,
                Sender = getMessageSender(message),
                IsSelf = isMessageSelf(message)
            };
            message.Tags.ToList().ForEach(item => chatMessage.Meta.Add(item));

            chatMessage = ProcessEmotes(chatMessage, message);

            context.PublishChatEvent(chatMessage);

            return Task.CompletedTask;
        }

        private Task UserNoticeEventHandler(IrcClient client, Message message)
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

            chatEvent = (LoyaltyEvent) ProcessEmotes(chatEvent, message);

            chatEvent.Topic = ChatTopics.Loyalty;

            context.PublishChatEvent(chatEvent);

            return Task.CompletedTask;
        }

        private Task WhisperEventHandler(IrcClient client, Message message)
        {
            var chatMessage = new ChatMessage() {
                ClientName = ClientName,
                ProviderName = ProviderName,
                TextContent = message.TrailingParameter,
                Sender = getMessageSender(message),
                IsSelf = isMessageSelf(message)
            };
            message.Tags.ToList().ForEach(item => chatMessage.Meta.Add(item));

            chatMessage = ProcessEmotes(chatMessage, message);

            context.PublishChatEvent(chatMessage);
            return Task.CompletedTask;
        }

        private ChatMessage ProcessEmotes(ChatMessage chatMessage, Message message)
        {
            message.Tags.TryGetValue("emotes", out string value);

            if (value != "" && value != null)
            {
                List<EmoteOccurance> emoteOccurances = new List<EmoteOccurance> { };
                Dictionary<string, Tuple<int, string>> keyValuePairs = new Dictionary<string, Tuple<int, string>> { };
                List<string> emotes = value.Split('/').ToList(); // / seperates emote types
                foreach (string emote in emotes)
                {
                    string[] splitEmoteColon = emote.Split(':'); // : seperates the ID from the indexes
                    if (splitEmoteColon.Length != 2 || !int.TryParse(splitEmoteColon[0], out int emoteId))
                        continue;
                    List<string> occurances = splitEmoteColon[1].Split(',').ToList(); // , seperates emote occurances
                    foreach (string occurance in occurances)
                    {
                        int.TryParse(occurance.Split('-')[0], out int firstIndex); // - seperates the start index from the end index
                        int.TryParse(occurance.Split('-')[1], out int secondIndex);
                        string name = string.Empty;
                        name = chatMessage.TextContent.Substring(firstIndex, (secondIndex + 1) - firstIndex);
                        emoteOccurances.Add(new EmoteOccurance { Emotes = new Tuple<string, int, int>(name, firstIndex, secondIndex) });
                        if (!keyValuePairs.ContainsKey(name))
                        {
                            keyValuePairs.Add(name, new Tuple<int, string>(emoteId, $"http://static-cdn.jtvnw.net/emoticons/v1/{emoteId}/3.0"));
                        }
                    }
                }
                chatMessage.Emote = new ChatMessage.Emotes
                {
                    Data = keyValuePairs,
                    Ranges = emoteOccurances
                };
                Debug.Assert(chatMessage.Emote.Ranges[0] != null);
            }
            else
            {
                chatMessage.Emote = new ChatMessage.Emotes
                {
                    Data = new Dictionary<string, Tuple<int, string>> { },
                    Ranges = new List<EmoteOccurance> { }
                };
                Debug.Assert(chatMessage.Emote != null);
            }
            return chatMessage;
        }
    }
}
