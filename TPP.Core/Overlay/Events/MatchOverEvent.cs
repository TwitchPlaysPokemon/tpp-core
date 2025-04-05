using System.Runtime.Serialization;
using TPP.Model;

namespace TPP.Core.Overlay.Events;

[DataContract]
public struct MatchOverEvent : IOverlayEvent
{
    public string OverlayEventType => "match_over";

    [DataMember(Name = "match_result")]
    public MatchResult MatchResult { get; set; }
}
