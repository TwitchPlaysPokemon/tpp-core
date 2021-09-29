using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TPP.Common;

namespace TPP.Persistence.MongoDB.Serializers;

/// <summary>
/// A serializer that represents an enum with the value of its <see cref="EnumMemberAttribute"/>.
/// </summary>
public abstract class EnumToStringUsingEnumMemberValue<T> : EnumToStringUsingTranslationMappingSerializer<T>
    where T : struct, Enum
{
    private static Dictionary<T, string> GenerateMapping() =>
        Enum.GetValues<T>().ToDictionary(
            e => e,
            e => e.GetEnumMemberValue() ?? throw new ArgumentException(
                $"enum value {e} does not have a EnumMember attribute with a value"));

    protected EnumToStringUsingEnumMemberValue() : base(GenerateMapping())
    {
    }
}
