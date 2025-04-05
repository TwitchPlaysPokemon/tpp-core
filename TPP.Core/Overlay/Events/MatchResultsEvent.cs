using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace TPP.Core.Overlay.Events;

[DataContract]
public struct Transaction
{
    [DataMember(Name = "change")] public long Change { get; set; }
    [DataMember(Name = "new_balance")] public long NewBalance { get; set; }
}

[DataContract]
public struct PokeyenResults
{
    [DataMember(Name = "transactions")] public IImmutableDictionary<string, Transaction> Transactions { get; set; }

    // TODO pokeyen_rank_deltas, seems to be unused

    // TODO no support for bet bonuses yet, but the overlay crashes if these fields aren't filled
    [DataMember(Name = "input_bonus_multipliers")]
    public IImmutableDictionary<string, long> InputBonusMultipliers =>
        Transactions.ToImmutableDictionary(kvp => kvp.Key, kvp => 0L);
    [DataMember(Name = "max_multiplier")]
    public double MaxMultiplier => 0D;
    [DataMember(Name = "adjusted_bet_bonuses")]
    public IImmutableDictionary<string, double> AdjustedBetBonuses =>
        Transactions.ToImmutableDictionary(kvp => kvp.Key, kvp => 0D);
}

[DataContract]
public struct MatchResultsEvent : IOverlayEvent
{
    public string OverlayEventType => "match_results";
    [DataMember(Name = "pokeyen_results")] public PokeyenResults PokeyenResults { get; set; }
}
