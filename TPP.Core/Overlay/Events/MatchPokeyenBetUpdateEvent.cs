using System.Collections.Immutable;
using System.Runtime.Serialization;
using TPP.Model;

namespace TPP.Core.Overlay.Events;

[DataContract]
public struct Bet
{
    [DataMember(Name = "amount")] public long Amount { get; set; }
    [DataMember(Name = "team")] public Side Team { get; set; }
    [DataMember(Name = "bet_bonus")] public long BetBonus { get; set; } // TODO
}

[DataContract]
public struct MatchPokeyenBetUpdateEvent : IOverlayEvent
{
    public string OverlayEventType => "match_pokeyen_bet_update";

    [DataMember(Name = "match_id")] public int MatchId { get; set; }
    [DataMember(Name = "odds")] public IImmutableDictionary<Side, double> Odds { get; set; }
    [DataMember(Name = "bet")] public Bet NewBet { get; set; }
    [DataMember(Name = "user")] public User NewBetUser { get; set; }
    [DataMember(Name = "default_action")] public string DefaultAction { get; set; }
    // unused: stats
    // used by betting graph, which is currently disabled: bet_counts
}
