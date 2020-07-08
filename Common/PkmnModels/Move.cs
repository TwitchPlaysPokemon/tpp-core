using System.Runtime.Serialization;

namespace Common.PkmnModels
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
        [DataMember(Name = "id")] public int Id { get; set; }
        [DataMember(Name = "name_id")] public string NameId { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "category")] public Category Category { get; set; }
        [DataMember(Name = "type")] public PokemonType Type { get; set; }
        [DataMember(Name = "accuracy")] public int Accuracy { get; set; }
        [DataMember(Name = "power")] public int Power { get; set; }
        [DataMember(Name = "pp")] public int Pp { get; set; }
        [DataMember(Name = "pp_ups")] public int PpUps { get; set; }
        [DataMember(Name = "target")] public MoveTarget Target { get; set; }
    }
}
