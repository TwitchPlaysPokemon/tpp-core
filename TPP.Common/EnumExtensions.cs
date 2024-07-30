using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace TPP.Common
{
    public static class EnumExtensions
    {
        public static string? GetEnumMemberValue(this Enum value)
        {
            return value.GetType()
                .GetTypeInfo()
                .DeclaredMembers
                .SingleOrDefault(x => x.Name == value.ToString())
                ?.GetCustomAttribute<EnumMemberAttribute>(false)
                ?.Value;
        }
    }
}
