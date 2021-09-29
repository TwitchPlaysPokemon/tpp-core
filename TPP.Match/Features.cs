using System.Collections.Immutable;

namespace TPP.Match;

public static class Features
{
    public enum Generation
    {
        Gen1 = 1, Gen2, Gen3, Gen4, Gen5, Gen6, Gen7, Gen8
    }

    public enum Capability
    {
        /// support for deliberate switching
        Switching,

        /// support for usage of arbitrary pokemon (hackmons, illegal EVs, etc.)
        Illegals,

        /// support for double battles
        Doubles,

        /// support for adjusting speed (e.g. animation and/or emulator speed in pbr)
        AdjustingSpeed,

        /// support for setting the stage/colosseum
        SetStage,

        /// support for setting the initial field effect
        SetFieldEffect,

        // TODO SetInitialHp
    }

    public record FeatureSet(
        Generation Generation,
        int MaxTeamMembers,
        IImmutableSet<Capability> Capabilities);
}
