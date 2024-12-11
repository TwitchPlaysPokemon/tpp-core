using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;

namespace TPP.Core.Utils;

/// <summary>
/// Converts between NodaTime <see cref="Instant"/> and unix epoch seconds as a 64-bit number.
/// E.g. during deserialization <c>1733865557</c> becomes <c>2024-12-10T21:19:17Z</c>.
/// </summary>
public class InstantAsUnixSecondsConverter : JsonConverter<Instant>
{
    public override Instant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Instant.FromUnixTimeSeconds(reader.GetInt64());
    public override void Write(Utf8JsonWriter writer, Instant value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.ToUnixTimeSeconds());
}
