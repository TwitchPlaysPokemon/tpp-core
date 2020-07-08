using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Common.PkmnModels
{
    [DataContract]
    public struct Pokeset
    {
        public Pokemon Pokemon { get; set; }

        [DataMember(Name = "ingamename")] public string Ingamename { get; set; }
        [DataMember(Name = "setname")] public string Setname { get; set; }
        [DataMember(Name = "biddable")] public bool Biddable { get; set; }
        [DataMember(Name = "hidden")] public bool Hidden { get; set; }
        [DataMember(Name = "rarity")] public float Rarity { get; set; }
        [DataMember(Name = "tags")] public IImmutableList<string> Tags { get; set; }
    }
}
