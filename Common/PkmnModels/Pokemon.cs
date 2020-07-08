using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Common.PkmnModels
{
    [DataContract]
    public struct Pokemon
    {
        [DataMember(Name = "displayname")] public string Name { get; set; }
        [DataMember(Name = "species")] public Species Species { get; set; }
        [DataMember(Name = "nature")] public Nature? Nature { get; set; }
        [DataMember(Name = "ability")] public Ability Ability { get; set; }
        [DataMember(Name = "level")] public int Level { get; set; }
        [DataMember(Name = "gender")] public Gender? Gender { get; set; }
        [DataMember(Name = "shiny")] public bool Shiny { get; set; }
        [DataMember(Name = "happiness")] public int Happiness { get; set; }
        [DataMember(Name = "item")] public Item Item { get; set; }
        [DataMember(Name = "ball")] public Item? Ball { get; set; }
        [DataMember(Name = "form")] public int Form { get; set; }
        [DataMember(Name = "moves")] public IImmutableList<Move> Moves { get; set; }
        [DataMember(Name = "hp_type")] public ElementalType? HpType { get; set; }
        [DataMember(Name = "evs")] public Stats Evs { get; set; }
        [DataMember(Name = "ivs")] public Stats Ivs { get; set; }
        [DataMember(Name = "stats")] public Stats Stats { get; set; }
        [DataMember(Name = "curr_hp")] public int CurrHp { get; set; }
        [DataMember(Name = "status")] public Status Status { get; set; }
    }
}
