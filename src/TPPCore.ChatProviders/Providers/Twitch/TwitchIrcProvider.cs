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
            await ircClient.SendMessage("CAP", "REQ", null,
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
            message.Tags.TryGetValue("emotes", out string value);

            if (value != "" && value != null)
            {
                List<EmoteOccurance> emoteOccurances = new List<EmoteOccurance> { };
                Dictionary<string, Tuple<int, string>> keyValuePairs = new Dictionary<string, Tuple<int, string>> { };
                List<string> emotes = value.Split('/').ToList(); // / seperates emote types
                foreach (string emote in emotes)
                {
                    int.TryParse(emote.Split(':')[0], out int emoteId); // : seperates the ID from the indexes
                    string indexes = emote.Split(':')[1];
                    if (emote.Contains(',')) // , seperates emote occurances
                    {
                        List<string> occurances = indexes.Split(',').ToList();
                        foreach (string occurance in occurances)
                        {
                            int.TryParse(occurance.Split('-')[0], out int firstIndex); // - seperates the start index from the end index
                            int.TryParse(occurance.Split('-')[1], out int secondIndex);
                            string name = string.Empty;
                            for (int i = firstIndex; i <= secondIndex; i++)
                            {
                                name += chatMessage.TextContent[i];
                            }
                            emoteOccurances.Add(new EmoteOccurance { Emotes = new Tuple<string, int, int>(name, firstIndex, secondIndex) });
                            if (!keyValuePairs.ContainsKey(name))
                            {
                                keyValuePairs.Add(name, new Tuple<int, string>(emoteId, $"http://static-cdn.jtvnw.net/emoticons/v1/{emoteId}/3.0"));
                            }
                        }
                    }
                    else
                    {
                        int.TryParse(indexes.Split('-')[0], out int firstIndex);
                        int.TryParse(indexes.Split('-')[1], out int secondIndex);
                        string name = string.Empty;
                        for (int i = firstIndex; i <= secondIndex; i++)
                        {
                            name += chatMessage.TextContent[i];
                        }
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
            } else
            {
                chatMessage.Emote = new ChatMessage.Emotes
                {
                    Data = new Dictionary<string, Tuple<int, string>> { },
                    Ranges = new List<EmoteOccurance> { }
                };
                Debug.Assert(chatMessage.Emote != null);
            }
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
            message.Tags.TryGetValue("emotes", out string value);

            if (value != "" && value != null)
            {
                List<EmoteOccurance> emoteOccurances = new List<EmoteOccurance> { };
                Dictionary<string, Tuple<int, string>> keyValuePairs = new Dictionary<string, Tuple<int, string>> { };
                List<string> emotes = value.Split('/').ToList(); // / seperates emote types
                foreach (string emote in emotes)
                {
                    int.TryParse(emote.Split(':')[0], out int emoteId); // : seperates the ID from the indexes
                    string indexes = emote.Split(':')[1];
                    if (emote.Contains(',')) // , seperates emote occurances
                    {
                        List<string> occurances = indexes.Split(',').ToList();
                        foreach (string occurance in occurances)
                        {
                            int.TryParse(occurance.Split('-')[0], out int firstIndex); // - seperates the start index from the end index
                            int.TryParse(occurance.Split('-')[1], out int secondIndex);
                            string name = string.Empty;
                            for (int i = firstIndex; i <= secondIndex; i++)
                            {
                                name += chatEvent.TextContent[i];
                            }
                            emoteOccurances.Add(new EmoteOccurance { Emotes = new Tuple<string, int, int>(name, firstIndex, secondIndex) });
                            if (!keyValuePairs.ContainsKey(name))
                            {
                                keyValuePairs.Add(name, new Tuple<int, string>(emoteId, $"http://static-cdn.jtvnw.net/emoticons/v1/{emoteId}/3.0"));
                            }
                        }
                    }
                    else
                    {
                        int.TryParse(indexes.Split('-')[0], out int firstIndex);
                        int.TryParse(indexes.Split('-')[1], out int secondIndex);
                        string name = string.Empty;
                        for (int i = firstIndex; i <= secondIndex; i++)
                        {
                            name += chatEvent.TextContent[i];
                        }
                        emoteOccurances.Add(new EmoteOccurance { Emotes = new Tuple<string, int, int>(name, firstIndex, secondIndex) });
                        if (!keyValuePairs.ContainsKey(name))
                        {
                            keyValuePairs.Add(name, new Tuple<int, string>(emoteId, $"http://static-cdn.jtvnw.net/emoticons/v1/{emoteId}/3.0"));
                        }
                    }
                }
                chatEvent.Emote = new LoyaltyEvent.Emotes
                {
                    Data = keyValuePairs,
                    Ranges = emoteOccurances
                };
                Debug.Assert(chatEvent.Emote.Ranges[0] != null);
            }
            else
            {
                chatEvent.Emote = new LoyaltyEvent.Emotes
                {
                    Data = new Dictionary<string, Tuple<int, string>> { },
                    Ranges = new List<EmoteOccurance> { }
                };
                Debug.Assert(chatEvent.Emote != null);
            }
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

            context.PublishChatEvent(chatMessage);
            return Task.CompletedTask;
        }
    }
}
