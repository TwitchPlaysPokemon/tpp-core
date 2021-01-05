using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Common.PkmnModels
{
    [DataContract]
    public struct Pokemon
    {
        [DataMember(Name = "displayname")] public string Name { get; init; }
        [DataMember(Name = "setname")] public string Setname { get; init; }
        [DataMember(Name = "species")] public Species Species { get; init; }
        [DataMember(Name = "nature")] public Nature? Nature { get; init; }
        [DataMember(Name = "ability")] public Ability Ability { get; init; }
        [DataMember(Name = "level")] public int Level { get; init; }
        [DataMember(Name = "gender")] public Gender? Gender { get; init; }
        [DataMember(Name = "shiny")] public bool Shiny { get; init; }
        [DataMember(Name = "happiness")] public int Happiness { get; init; }
        [DataMember(Name = "item")] public Item Item { get; init; }
        [DataMember(Name = "ball")] public Item? Ball { get; init; }
        [DataMember(Name = "form")] public int Form { get; init; }
        [DataMember(Name = "moves")] public IImmutableList<Move> Moves { get; init; }
        [DataMember(Name = "hp_type")] public PokemonType? HpType { get; init; }
        [DataMember(Name = "evs")] public Stats Evs { get; init; }
        [DataMember(Name = "ivs")] public Stats Ivs { get; init; }
        [DataMember(Name = "stats")] public Stats Stats { get; init; }
        [DataMember(Name = "curr_hp")] public int CurrHp { get; init; }
        [DataMember(Name = "status")] public Status Status { get; init; }
    }
}
