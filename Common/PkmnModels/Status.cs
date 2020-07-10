using System.Runtime.Serialization;

namespace Common.PkmnModels
{
    [DataContract]
    public struct NonvolatileStatus
    {
        [DataMember(Name = "brn")] public bool Burn { get; set; }
        [DataMember(Name = "frz")] public bool Freeze { get; set; }
        [DataMember(Name = "par")] public bool Paralysis { get; set; }
        [DataMember(Name = "psn")] public bool Poison { get; set; }
        [DataMember(Name = "tox")] public int BadPoison { get; set; }
        [DataMember(Name = "slp")] public int Sleep { get; set; }
    }

    [DataContract]
    public struct Status
    {
        [DataMember(Name = "nonvolatile")] public NonvolatileStatus Nonvolatile { get; set; }
        // volatile may get added in the future if the need arises
    }
}
