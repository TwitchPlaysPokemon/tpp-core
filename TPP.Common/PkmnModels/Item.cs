using System.Runtime.Serialization;

namespace TPP.Common.PkmnModels
{
    [DataContract]
    public struct Item
    {
        [DataMember(Name = "id")] public int Id { get; init; }
        [DataMember(Name = "name")] public string Name { get; init; }
        [DataMember(Name = "description")] public string Description { get; init; }
    }
}
