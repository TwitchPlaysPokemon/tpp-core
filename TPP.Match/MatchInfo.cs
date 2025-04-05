using System.Collections.Immutable;
using TPP.Model;

namespace TPP.Match;

public record MatchInfo(
    IImmutableList<Pokemon> TeamBlue,
    IImmutableList<Pokemon> TeamRed)
{
    public string? Stage { get; init; }
    public string? FieldEffect { get; init; }
    public float Speed { get; init; } = 1f;
}
