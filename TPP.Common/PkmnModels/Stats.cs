using System.Runtime.Serialization;

namespace TPP.Common.PkmnModels
{
    [DataContract]
    public enum Stat
    {
        [EnumMember(Value = "hp")] Hp,
        [EnumMember(Value = "atk")] Atk,
        [EnumMember(Value = "def")] Def,
        [EnumMember(Value = "spA")] SpA,
        [EnumMember(Value = "spD")] SpD,
        [EnumMember(Value = "spe")] Spe,
    }

    [DataContract]
    public struct Stats
    {
        [DataMember(Name = "hp")] public int Hp { get; init; }
        [DataMember(Name = "atk")] public int Atk { get; init; }
        [DataMember(Name = "def")] public int Def { get; init; }
        [DataMember(Name = "spA")] public int SpA { get; init; }
        [DataMember(Name = "spD")] public int SpD { get; init; }
        [DataMember(Name = "spe")] public int Spe { get; init; }
    }
}
