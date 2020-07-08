using System.Runtime.Serialization;

namespace Common.PkmnModels
{
    [DataContract]
    public enum Category
    {
        [EnumMember(Value = "Physical")] Physical,
        [EnumMember(Value = "Special")] Special,
        [EnumMember(Value = "Status")] Status,
    }
}
