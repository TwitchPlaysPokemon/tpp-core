using System.Collections.Generic;

namespace TPPCore.Service.Emotes
{
    public class TwitchEmote : EmoteInfo
    {
        public TwitchEmote(int id, string code)
        {
            Id = id;
            Code = code;
            ImageUrls = new List<string> { $"https://static-cdn.jtvnw.net/emoticons/v1/{id}/1.0", $"https://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0", $"https://static-cdn.jtvnw.net/emoticons/v1/{id}/3.0" };
        }
    }
}
