using System.Collections.Generic;
using System.Runtime.Serialization;
using TPP.Model;

namespace TPP.Core.Overlay.Events;

[DataContract]
public struct NewDonationEvent : IOverlayEvent
{
    public string OverlayEventType => "new_donation";

    [DataContract]
    public struct DonationInfo
    {
        [DataMember(Name = "name")] public string Username { get; set; }
        [DataMember(Name = "cents")] public int Cents { get; set; }
        [DataMember(Name = "message")] public string? Message { get; set; }
    }

    [DataMember(Name = "donation")]
    public DonationInfo Donation { get; set; }

    [DataMember(Name = "emotes")]
    public List<EmoteInfo> Emotes { get; set; }

    // E.g. {200: ["1h", "2h"], 500: ["6h"]}
    // In reality, the dictionary always ever has 1 key: the donation's cents amount. So this could theoretically just
    // be a List<string>, but it's this way for backwards-compatibility with the old overlay.
    [DataMember(Name = "record_donations")]
    public Dictionary<int, List<string>> RecordDonations { get; set; }

    // old core also had "potential_token_winners" and "token_winners", but those seemed unused
}
