using System.Runtime.Serialization;

namespace TPP.Common.PkmnModels
{
    [DataContract]
    public struct NonvolatileStatus
    {
        [DataMember(Name = "brn")] public bool Burn { get; init; }
        [DataMember(Name = "frz")] public bool Freeze { get; init; }
        [DataMember(Name = "par")] public bool Paralysis { get; init; }
        [DataMember(Name = "psn")] public bool Poison { get; init; }
        [DataMember(Name = "tox")] public int BadPoison { get; init; }
        [DataMember(Name = "slp")] public int Sleep { get; init; }
    }

    [DataContract]
    public struct Status
    {
        [DataMember(Name = "nonvolatile")] public NonvolatileStatus Nonvolatile { get; set; }
        // volatile may get added in the future if the need arises
    }
}
