using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Common.PkmnModels
{
    [DataContract]
    public struct Species
    {
        [DataMember(Name = "id")] public int Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "basestats")] public Stats Basestats { get; set; }
        [DataMember(Name = "types")] public IList<string> Types { get; set; }
        // optional additional pokedex data
        [DataMember(Name = "color")] public string? Color { get; set; }
        [DataMember(Name = "gender_ratios")] public IList<float>? GenderRatios { get; set; }
    }
}
