using TPPCore.ChatProviders.DataModels;

namespace TPPCore.Service.Emotes
{
    internal class TwitchEmote : EmoteInfo
    {
        public TwitchEmote(string id, string code)
        {
            Id = id;
            Code = code;
        }
        public override string[] ImageUrls => new[] { $"https://static-cdn.jtvnw.net/emoticons/v1/{Id}/1.0", $"https://static-cdn.jtvnw.net/emoticons/v1/{Id}/2.0", $"https://static-cdn.jtvnw.net/emoticons/v1/{Id}/3.0" };
    }
}
