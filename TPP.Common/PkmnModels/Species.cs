using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TPP.Common.PkmnModels
{
    [DataContract]
    public struct Species
    {
        [DataMember(Name = "id")] public int Id { get; init; }
        [DataMember(Name = "name")] public string Name { get; init; }
        [DataMember(Name = "basestats")] public Stats Basestats { get; init; }
        [DataMember(Name = "types")] public IList<string> Types { get; init; }
        // optional additional pokedex data
        [DataMember(Name = "color")] public string? Color { get; init; }
        [DataMember(Name = "gender_ratios")] public IList<float>? GenderRatios { get; init; }
    }
}
