using System.Runtime.Serialization;

namespace Common.PkmnModels
{
    [DataContract]
    public enum Gender
    {
        [EnumMember(Value = "m")] Male,
        [EnumMember(Value = "f")] Female,
    }
}
