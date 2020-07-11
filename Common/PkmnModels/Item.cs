using System.Runtime.Serialization;

namespace Common.PkmnModels
{
    [DataContract]
    public struct Item
    {
        [DataMember(Name = "id")] public int Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "description")] public string Description { get; set; }
    }
}
