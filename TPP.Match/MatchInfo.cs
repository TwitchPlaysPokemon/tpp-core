using System.Collections.Immutable;
using System.Runtime.Serialization;
using TPP.Common.PkmnModels;

namespace TPP.Match
{
    [DataContract]
    public enum Side
    {
        [EnumMember(Value = "blue")] Blue,
        [EnumMember(Value = "red")] Red,
    }

    public record MatchResult(Side? Winner);

    public record MatchInfo(
        IImmutableList<Pokemon> TeamBlue,
        IImmutableList<Pokemon> TeamRed)
    {
        public string? Stage { get; init; }
        public string? FieldEffect { get; init; }
        public float Speed { get; init; } = 1f;
    }
}
