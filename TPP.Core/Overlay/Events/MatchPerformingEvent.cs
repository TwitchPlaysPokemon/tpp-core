using System.Runtime.Serialization;

namespace TPP.Core.Overlay.Events;

[DataContract]
public struct MatchPerformingEvent : IOverlayEvent
{
    public string OverlayEventType => "overlay_match_performing"; // TODO fix horrible hack in old core

    [DataMember(Name = "teams")] public Teams Teams { get; set; }
}
