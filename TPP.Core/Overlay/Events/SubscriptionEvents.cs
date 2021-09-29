using System.Collections.Immutable;
using System.Runtime.Serialization;
using TPP.Model;

namespace TPP.Core.Overlay.Events;

[DataContract]
public struct EmoteInfo
{
    [DataMember(Name = "id")] public string Id { get; set; }
    [DataMember(Name = "code")] public string Code { get; set; }
    [DataMember(Name = "x1")] public string X1 { get; set; }
    [DataMember(Name = "x2")] public string X2 { get; set; }
    [DataMember(Name = "x3")] public string X3 { get; set; }

    public static EmoteInfo FromOccurence(EmoteOccurrence emote) => new()
    {
        Code = emote.Code,
        Id = emote.Id,
        // see https://dev.twitch.tv/docs/irc/tags#privmsg-twitch-tags
        X1 = $"http://static-cdn.jtvnw.net/emoticons/v1/{emote.Id}/1.0",
        X2 = $"http://static-cdn.jtvnw.net/emoticons/v1/{emote.Id}/2.0",
        X3 = $"http://static-cdn.jtvnw.net/emoticons/v1/{emote.Id}/3.0",
    };
}

[DataContract]
public struct NewSubscriber : IOverlayEvent
{
    public string OverlayEventType => "new_subscriber";

    [DataMember(Name = "user")] public User User { get; set; }
    [DataMember(Name = "message")] public string? SubMessage { get; set; }
    [DataMember(Name = "emotes")] public IImmutableList<EmoteInfo> Emotes { get; set; }
    [DataMember(Name = "share_sub")] public bool ShareSub { get; set; }
}
