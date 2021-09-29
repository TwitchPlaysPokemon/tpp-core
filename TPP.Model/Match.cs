using System;
using System.Runtime.Serialization;

namespace TPP.Model;

/// Describes under what circumstances switching is allowed.
[DataContract]
public enum SwitchingPolicy
{
    /// Switching is allowed under all circumstances.
    [EnumMember(Value = "always")] Always,
    /// Switching is never allowed.
    [EnumMember(Value = "never")] Never,
    /// Players can only switch pokemon during a regular move selection.
    [EnumMember(Value = "only_regular")] OnlyRegular,
    /// Players can only influence what pokemon to switch to after a faint.
    [EnumMember(Value = "only_faint")] OnlyFaint,
    /// Players can only influence what pokemon to switch to after an event that isn't a regular switch or a faint,
    /// e.g. after using Baton Pass.
    [EnumMember(Value = "only_other")] OnlyOther,
}

/// Describes how the moves to use are being chosen.
[DataContract]
public enum MoveSelectingPolicy
{
    /// Always explicitly select the move to use.
    [EnumMember(Value = "always")] Always,
    /// Automatically choose a move to use at random.
    [EnumMember(Value = "random")] Random,
}

/// Describes how the pokemon to target with an attack are being selected.
[DataContract]
public enum TargetingPolicy
{
    /// Always explicitly select the target pokemon.
    [EnumMember(Value = "always")] Always,
    /// Automatically select a target pokemon at random.
    [EnumMember(Value = "random")] Random,
    /// No target selection is available (1v1 battle).
    [EnumMember(Value = "disabled")] Disabled,
}

/// Describes in what style the battle is performed.
[DataContract]
public enum BattleStyle
{
    /// 1v1 battle
    [EnumMember(Value = "singles")] Singles,
    /// 2v2 battle
    [EnumMember(Value = "doubles")] Doubles,
    // There's more, e.g. showdown multi-battles, but those aren't supported (yet).
}

/// ID of the game played for a match
[DataContract]
public enum GameId
{
    [EnumMember(Value = "coinflip")] Coinflip,
    [EnumMember(Value = "pbr")] PokemonBattleRevolution,
    [EnumMember(Value = "ps2")] PokemonStadium2,
    [EnumMember(Value = "showdown")] PokemonShowdown,
}

[DataContract]
public enum Side
{
    [EnumMember(Value = "blue")] Blue,
    [EnumMember(Value = "red")] Red,
}

[DataContract]
public enum MatchResult
{
    [EnumMember(Value = "blue")] Blue,
    [EnumMember(Value = "red")] Red,
    [EnumMember(Value = "draw")] Draw,
}

public static class MatchResultExtensions
{
    public static Side? ToSide(this MatchResult matchResult) => matchResult switch
    {
        MatchResult.Blue => Side.Blue,
        MatchResult.Red => Side.Red,
        MatchResult.Draw => null,
        _ => throw new ArgumentOutOfRangeException(nameof(matchResult), matchResult, null)
    };
}
