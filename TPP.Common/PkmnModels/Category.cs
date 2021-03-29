using System.Runtime.Serialization;

namespace TPP.Common.PkmnModels
{
    [DataContract]
    public enum Category
    {
        [EnumMember(Value = "Physical")] Physical,
        [EnumMember(Value = "Special")] Special,
        [EnumMember(Value = "Status")] Status,
    }
}
