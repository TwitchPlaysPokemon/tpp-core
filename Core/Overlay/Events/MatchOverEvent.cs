using System.Runtime.Serialization;

namespace Core.Overlay.Events
{
    [DataContract]
    public struct MatchOverEvent : IOverlayEvent
    {
        public string OverlayEventType => "match_over";

        // TODO fix: overlay expects 0, 1 or "draw"...
        [DataMember(Name = "match_result")] public object MatchResult { get; set; }
    }
}
