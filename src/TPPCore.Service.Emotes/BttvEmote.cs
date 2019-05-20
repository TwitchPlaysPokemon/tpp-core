using TPPCore.ChatProviders.DataModels;

namespace TPPCore.Service.Emotes
{
    internal class BttvEmote : EmoteInfo
    {
        public BttvEmote(string id, string code)
        {
            Id = id;
            Code = code;
        }

        public override string[] ImageUrls => new[] { $"https://cdn.betterttv.net/emote/{Id}/1x", $"https://cdn.betterttv.net/emote/{Id}/2x", $"https://cdn.betterttv.net/emote/{Id}/3x" };
    }
}
