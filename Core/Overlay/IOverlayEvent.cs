using System.Runtime.Serialization;

namespace Core.Overlay
{
    /// Instances of this interface can be sent to the (legacy) overlay through a <see cref="OverlayConnection"/>.
    public interface IOverlayEvent
    {
        [IgnoreDataMember] public string OverlayEventType { get; }
    }
}
