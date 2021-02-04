using System.Runtime.Serialization;

namespace TPP.Common.PkmnModels
{
    [DataContract]
    public enum MoveTarget
    {
        [EnumMember(Value = "self")] Self,
        [EnumMember(Value = "scripted")] Scripted,
        [EnumMember(Value = "randomNormal")] RandomNormal,
        [EnumMember(Value = "normal")] Normal,
        [EnumMember(Value = "any")] Any,
        [EnumMember(Value = "adjacentFoe")] AdjacentFoe,
        [EnumMember(Value = "foeSide")] FoeSide,
        [EnumMember(Value = "allAdjacentFoes")] AllAdjacentFoes,
        [EnumMember(Value = "allAdjacent")] AllAdjacent,
        [EnumMember(Value = "all")] All,
        [EnumMember(Value = "adjacentAllyOrSelf")] AdjacentAllyOrSelf,
        [EnumMember(Value = "adjacentAlly")] AdjacentAlly,
        [EnumMember(Value = "allyTeam")] AllyTeam,
        [EnumMember(Value = "allySide")] AllySide,
    }

    [DataContract]
    public struct Move
    {
        [DataMember(Name = "id")] public int Id { get; init; }
        [DataMember(Name = "name_id")] public string NameId { get; init; }
        [DataMember(Name = "displayname")] public string Name { get; init; }
        [DataMember(Name = "category")] public Category Category { get; init; }
        [DataMember(Name = "type")] public PokemonType Type { get; init; }
        [DataMember(Name = "accuracy")] public int Accuracy { get; init; }
        [DataMember(Name = "power")] public int Power { get; init; }
        [DataMember(Name = "pp")] public int Pp { get; init; }
        [DataMember(Name = "pp_ups")] public int PpUps { get; init; }
        [DataMember(Name = "target")] public MoveTarget Target { get; init; }
    }
}
