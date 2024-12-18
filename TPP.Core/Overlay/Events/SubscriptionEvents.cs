using System.Collections.Immutable;
using System.Runtime.Serialization;
using TPP.Core.Overlay.Events.Common;
using TPP.Model;

namespace TPP.Core.Overlay.Events
{
    [DataContract]
    public struct NewSubscriber : IOverlayEvent
    {
        public string OverlayEventType => "new_subscriber";

        [DataMember(Name = "user")] public User User { get; set; }
        [DataMember(Name = "message")] public string? SubMessage { get; set; }
        [DataMember(Name = "emotes")] public IImmutableList<EmoteInfo> Emotes { get; set; }
        [DataMember(Name = "share_sub")] public bool ShareSub { get; set; }
    }
}
