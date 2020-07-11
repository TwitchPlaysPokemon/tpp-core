using System.Runtime.Serialization;

namespace Common.PkmnModels
{
    [DataContract]
    public struct Ability
    {
        [DataMember(Name = "id")] public int Id { get; init; }
        [DataMember(Name = "name")] public string Name { get; init; }
        [DataMember(Name = "description")] public string Description { get; init; }
    }
}
