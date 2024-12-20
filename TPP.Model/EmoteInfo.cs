using System.Runtime.Serialization;

namespace TPP.Model;

[DataContract]
public struct EmoteInfo
{
    [DataMember(Name = "id")] public string Id { get; set; }
    [DataMember(Name = "code")] public string Code { get; set; }
    [DataMember(Name = "x1")] public string X1 { get; set; }
    [DataMember(Name = "x2")] public string X2 { get; set; }
    [DataMember(Name = "x3")] public string X3 { get; set; }

    public static EmoteInfo FromIdAndCode(string id, string code) => new()
    {
        Code = code,
        Id = id,
        // see https://dev.twitch.tv/docs/irc/tags#privmsg-twitch-tags
        X1 = $"https://static-cdn.jtvnw.net/emoticons/v2/{id}/static/light/1.0",
        X2 = $"https://static-cdn.jtvnw.net/emoticons/v2/{id}/static/light/2.0",
        X3 = $"https://static-cdn.jtvnw.net/emoticons/v2/{id}/static/light/3.0",
    };

    public override string ToString() =>
        $"Emote({nameof(Id)}: {Id}, {nameof(Code)}: {Code})";
}
