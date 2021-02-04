using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using TPP.Match;

namespace TPP.Core.Overlay.Events
{
    /// <summary>
    /// Serializes a <see cref="MatchResult"/> object as a string of either "blue", "red" or "draw".
    /// </summary>
    internal class MatchResultConverter : JsonConverter<MatchResult>
    {
        public override void WriteJson(JsonWriter writer, MatchResult? value, JsonSerializer serializer)
        {
            if (value == null) throw new NullReferenceException($"'{nameof(value)}' cannot be null!");
            serializer.Serialize(writer, value.Winner switch
            {
                Side.Blue => "blue",
                Side.Red => "red",
                null => "draw",
                _ => throw new ArgumentException($"unexpected match result side '{value.Winner}'")
            });
        }

        public override MatchResult ReadJson(JsonReader reader, Type objectType, MatchResult? existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            string? resultStr = serializer.Deserialize<string>(reader);
            return resultStr switch
            {
                "blue" => new MatchResult(Side.Blue),
                "red" => new MatchResult(Side.Red),
                "draw" => new MatchResult(null),
                _ => throw new ArgumentException($"unexpected match result string '{resultStr}'")
            };
        }
    }

    [DataContract]
    public struct MatchOverEvent : IOverlayEvent
    {
        public string OverlayEventType => "match_over";

        [DataMember(Name = "match_result")]
        [JsonConverter(typeof(MatchResultConverter))]
        public MatchResult MatchResult { get; set; }
    }
}
