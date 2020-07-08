using System.Runtime.Serialization;

namespace Common.PkmnModels
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
        [DataMember(Name = "hp")] public int Hp { get; set; }
        [DataMember(Name = "atk")] public int Atk { get; set; }
        [DataMember(Name = "def")] public int Def { get; set; }
        [DataMember(Name = "spA")] public int SpA { get; set; }
        [DataMember(Name = "spD")] public int SpD { get; set; }
        [DataMember(Name = "spe")] public int Spe { get; set; }
    }
}
