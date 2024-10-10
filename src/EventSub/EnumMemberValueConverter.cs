using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common;

namespace EventSub;

/// <summary>
/// Custom json converter that supports serializing and deserializing enums to and from the string each member value
/// specifies via <see cref="EnumMemberAttribute"/>. The enum itself must have <see cref="DataContractAttribute"/>.
/// Starting with dotnet 9, we can probably use [JsonStringEnumMemberName] instead, see https://github.com/dotnet/runtime/issues/74385#issuecomment-2220667024
/// </summary>
public class EnumMemberValueConverter : JsonConverter<Enum>
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsEnum && typeToConvert.GetCustomAttribute<DataContractAttribute>() != null;

    public override Enum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string valueStr = reader.GetString() ?? throw new JsonException(typeToConvert + " must not be null");
        foreach (Enum value in Enum.GetValues(typeToConvert))
            if (value.GetEnumMemberValue() == valueStr)
                return value;
        throw new JsonException($"Unknown {typeToConvert}: {valueStr}");
    }

    public override void Write(Utf8JsonWriter writer, Enum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.GetEnumMemberValue());
}
