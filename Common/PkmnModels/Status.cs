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
    public struct VolatileStatus
    {
        [DataMember(Name = "cnf")] public int Cnf { get; set; }
        [DataMember(Name = "cur")] public bool Cur { get; set; }
        [DataMember(Name = "foc")] public bool Foc { get; set; }
        [DataMember(Name = "inf")] public bool Inf { get; set; }
        [DataMember(Name = "tau")] public bool Tau { get; set; }
        [DataMember(Name = "tor")] public bool Tor { get; set; }
    }

    [DataContract]
    public struct Status
    {
        [DataMember(Name = "nonvolatile")] public NonvolatileStatus Nonvolatile { get; set; }
        [DataMember(Name = "volatile")] public VolatileStatus Volatile { get; set; }
    }
}
