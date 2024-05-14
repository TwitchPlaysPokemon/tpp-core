using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TPP.Inputting
{
    public enum GameSpecificAlias
    {
        [EnumMember(Value = "dppt")] Platinum,
        [EnumMember(Value = "hgss")] HeartGold,
        [EnumMember(Value = "bw")] BlackWhite,
        [EnumMember(Value = "bw2")] BlackWhite2,
        [EnumMember(Value = "xy")] XY,
        [EnumMember(Value = "oras")] OmegaRuby,
        [EnumMember(Value = "sm")] SunMoon,
        [EnumMember(Value = "usum")] UltraSun,
    }

    public static class GameSpecificAliases
    {
        public static InputParserBuilder AddGameAliases(this InputParserBuilder builder, GameSpecificAlias game) =>
            game switch
            {
                GameSpecificAlias.Platinum => builder.AddPlatinumAliases(),
                GameSpecificAlias.HeartGold => builder.AddPlatinumAliases(), // Same as Platinum, but may need fine tuning
                GameSpecificAlias.BlackWhite => builder.AddBWAliases(),
                GameSpecificAlias.BlackWhite2 => builder.AddBW2Aliases(),
                GameSpecificAlias.XY => builder.AddXYAliases(),
                GameSpecificAlias.OmegaRuby => builder.AddORASAliases(),
                GameSpecificAlias.SunMoon => builder.AddUSUMAliases(), // Same as Ultra Sun/Ultra Moon, but may need fine tuning
                GameSpecificAlias.UltraSun => builder.AddUSUMAliases(),
            };

        private static InputParserBuilder AddPlatinumAliases(this InputParserBuilder builder) =>
            builder.AliasedTouchscreenInputs(
                ("run", 111, 190),
                ("switch", 210, 155),
                ("bag", 4, 190),
                ("poke1", 111, 1),
                ("poke2", 250, 22),
                ("poke3", 3, 72),
                ("poke4", 253, 80),
                ("poke5", 1, 116),
                ("poke6", 250, 116),
                ("move1", 5, 23),
                ("move2", 252, 78),
                ("move3", 126, 94),
                ("move4", 250, 116),
                ("reuse", 38, 189),
                ("heal", 1, 46),
                ("throw", 250, 7),
                ("status", 0, 116),
                ("items", 253, 80)
            );

        private static InputParserBuilder AddBWAliases(this InputParserBuilder builder) =>
            builder.AliasedTouchscreenInputs(
                ("move1", 2, 72),
                ("move2", 252, 32),
                ("move3", 2, 124),
                ("move4", 248, 90),
                ("reuse", 79, 190),
                ("heal", 41, 20),
                ("throw", 225, 20),
                ("status", 120, 132),
                ("items", 254, 90),
                ("bag", 3, 170),
                ("run", 127, 151),
                ("switch", 210, 190),
                ("poke1", 10, 5),
                ("poke2", 149, 10),
                ("poke3", 6, 75),
                ("poke4", 250, 75),
                ("poke5", 5, 98),
                ("poke6", 130, 150)
            );

        private static InputParserBuilder AddBW2Aliases(this InputParserBuilder builder) =>
            builder.AliasedTouchscreenInputs(
                ("move1", 2, 72),
                ("move2", 252, 32),
                ("move3", 2, 124),
                ("move4", 248, 90),
                ("reuse", 79, 190),
                ("heal", 41, 20),
                ("throw", 225, 20),
                ("status", 120, 132),
                ("items", 254, 90),
                ("bag", 3, 170),
                ("run", 127, 161),
                ("switch", 210, 190),
                ("poke1", 10, 5),
                ("poke2", 149, 10),
                ("poke3", 6, 75),
                ("poke4", 250, 75),
                ("poke5", 5, 98),
                ("poke6", 130, 150)
            );
        
        private static InputParserBuilder AddXYAliases(this InputParserBuilder builder) =>
            builder.AliasedTouchscreenInputs(
                ("move1", 40, 70),
                ("move2", 279, 70),
                ("move3", 40, 130),
                ("move4", 279, 130),
                ("reuse", 25, 210),
                ("heal", 45, 25),
                ("throw", 274, 25),
                ("status", 45, 164),
                ("items", 274, 164),
                ("bag", 20, 234),
                ("run", 159, 234),
                ("switch", 249, 229),
                ("poke1", 20, 20),
                ("poke2", 299, 30),
                ("poke3", 20, 90),
                ("poke4", 299, 100),
                ("poke5", 20, 160),
                ("poke6", 299, 170),
                ("mega", 159, 190)
            );

        private static InputParserBuilder AddORASAliases(this InputParserBuilder builder) =>
            builder.AliasedTouchscreenInputs(
                ("move1", 40, 70),
                ("move2", 279, 70),
                ("move3", 40, 130),
                ("move4", 279, 130),
                ("reuse", 25, 220),
                ("heal", 45, 25),
                ("throw", 274, 40),
                ("status", 45, 149),
                ("items", 274, 164),
                ("bag", 20, 234),
                ("run", 159, 234),
                ("switch", 249, 229),
                ("poke1", 20, 20),
                ("poke2", 299, 30),
                ("poke3", 20, 90),
                ("poke4", 299, 100),
                ("poke5", 20, 160),
                ("poke6", 299, 170),
                ("mega", 159, 190)
            );

        private static InputParserBuilder AddUSUMAliases(this InputParserBuilder builder) =>
            builder.AliasedTouchscreenInputs(
                ("move1", 315, 50),
                ("move2", 315, 100),
                ("move3", 315, 150),
                ("move4", 315, 180),
                ("reuse", 77, 188),
                ("heal", 80, 20),
                ("throw", 160, 20),
                ("status", 70, 140),
                ("items", 140, 140),
                ("bag", 40, 160),
                ("run", 130, 200),
                ("switch", 20, 80),
                ("poke1", 20, 30),
                ("poke2", 20, 60),
                ("poke3", 20, 90),
                ("poke4", 20, 120),
                ("poke5", 20, 150),
                ("poke6", 20, 180)
            );
    }
}
