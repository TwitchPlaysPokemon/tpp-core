using System.Runtime.Serialization;

namespace TPP.Common.PkmnModels
{
    [DataContract]
    public enum Gender
    {
        [EnumMember(Value = "m")] Male,
        [EnumMember(Value = "f")] Female,
    }
}
