using System;
using System.Linq;
using System.Runtime.Serialization;
using TPP.Common;
using TPP.Inputting;

namespace TPP.Core
{
    [DataContract]
    public enum ButtonProfile
    {
        [EnumMember(Value = "gb")] GameBoy,
        [EnumMember(Value = "gba")] GameBoyAdvance,
        [EnumMember(Value = "nds")] NintendoDS,
        [EnumMember(Value = "3ds")] Nintendo3DS,

        [EnumMember(Value = "n3dsoffset")] NintendoDSOn3DSOffset, // Nintendo DS title launched on 3DS while holding Start or Select
        [EnumMember(Value = "n3dsscaled")] NintendoDSOn3DSScaled, // Nintendo DS title launched on 3DS without holding Start or Select

        [EnumMember(Value = "nes")] NES,
        [EnumMember(Value = "snes2nes")] SNEStoNES,
        [EnumMember(Value = "snes")] SNES,
        [EnumMember(Value = "n64")] N64,
        [EnumMember(Value = "gc")] GameCube,
        [EnumMember(Value = "switch")] Switch,

        [EnumMember(Value = "sgb")] SuperGameBoy, // GameBoy on SNES
        [EnumMember(Value = "gbt")] GameBoyTower, // GameBoy on N64 (Pokemon Stadium)
        [EnumMember(Value = "gbp")] GameBoyPlayer, // GameBoy Advance on GameCube

        [EnumMember(Value = "dualgb")] DualGameBoy,
        [EnumMember(Value = "dualnes")] DualNES,
        [EnumMember(Value = "dualsnes2nes")] DualSNEStoNES,
        [EnumMember(Value = "dualsnes")] DualSNES,
        [EnumMember(Value = "dualn64")] DualN64,
        [EnumMember(Value = "dualgc")] DualGameCube,
        [EnumMember(Value = "dualnds")] DualNintendoDS,
        [EnumMember(Value = "dual3ds")] DualNintendo3DS,
        [EnumMember(Value = "dualswitch")] DualSwitch,

        [EnumMember(Value = "dualsgb")] DualSuperGameBoy,
        [EnumMember(Value = "dualgbt")] DualGameBoyTower,
        [EnumMember(Value = "dualgbp")] DualGameBoyPlayer,

        [EnumMember(Value = "dualn3dsoffset")] DualNintendoDSOnDSOffset, // Nintendo DS title launched on 3DS while holding Start or Select
        [EnumMember(Value = "dualn3dsscaled")] DualNintendoDSOnDSScaled, // Nintendo DS title launched on 3DS without holding Start or Select

        [EnumMember(Value = "dualswitchgb")] DualSwitchButAlsoIsGB, // Needed for 2024/04/01 until we can have different sides running different button profiles

        [EnumMember(Value = "nestris")] NEStris, // (S)NES without Start
    }

    public static class ButtonProfileExtensions
    {
        public static string GetProfileName(this ButtonProfile profile) =>
            profile.GetEnumMemberValue()
            ?? throw new ArgumentException("profile is missing EnumMember for profile name: " + profile);

        public static bool IsDual(this ButtonProfile profile) =>
            profile.GetProfileName().StartsWith("dual", StringComparison.OrdinalIgnoreCase);

        public static bool HasTouchscreen(this ButtonProfile profile) =>
            profile.ToInputParserBuilder().HasTouchscreen;

        public static ButtonProfile? ToDual(this ButtonProfile profile)
        {
            if (profile.IsDual()) throw new ArgumentException("profile is already dual: " + profile);
            string expectedName = "dual" + profile.GetProfileName();
            return Enum
                .GetValues<ButtonProfile>()
                .Where(p => p.IsDual())
                .Where(p => p.GetProfileName().Equals(expectedName, StringComparison.OrdinalIgnoreCase))
                .Cast<ButtonProfile?>().FirstOrDefault();
        }

        public static ButtonProfile? ToNonDual(this ButtonProfile profile)
        {
            if (!profile.IsDual()) throw new ArgumentException("profile is not dual: " + profile);
            return Enum
                .GetValues<ButtonProfile>()
                .Where(p => !p.IsDual())
                .Where(p => p.ToDual() == profile)
                .Cast<ButtonProfile?>().FirstOrDefault();
        }

        public static InputParserBuilder ToInputParserBuilder(this ButtonProfile profile)
        {
            InputParserBuilder inputParserBuilder = profile switch
            {
                ButtonProfile.GameBoy => InputParserBuilder.FromBare()
                    .Buttons("a", "b", "start", "select")
                    .DPad()
                    // Max set length of 3 is too short to perform Soft Reset (A+B+Start+Select)
                    .LengthRestrictions(maxSetLength: 3, maxSequenceLength: 1),
                ButtonProfile.GameBoyAdvance => ButtonProfile.GameBoy.ToInputParserBuilder()
                    .Buttons("l", "r"),
                ButtonProfile.NintendoDS => ButtonProfile.SNES.ToInputParserBuilder()
                    .Touchscreen(width: 256, height: 192, multitouch: false, allowDrag: true),
                ButtonProfile.Nintendo3DS => ButtonProfile.SNES.ToInputParserBuilder()
                    .SimpleAliasedDPad("d", "")
                    .AnalogStick("c", true)
                    .SimpleAliasedAnalogStick("", "c", true)
                    .Touchscreen(width: 320, height: 240, multitouch: false, allowDrag: true)
                    // Prevent Soft Reset in 3DS Pokemon Games (L+R+Start/Select) as well as Luma3DS and NTR menu shortcuts
                    .Conflicts(("l", "select"), ("l", "start")),

                ButtonProfile.NintendoDSOn3DSOffset => ButtonProfile.SNES.ToInputParserBuilder()
                    .Touchscreen(width: 256, height: 192, multitouch: false, allowDrag: true, xOffset: 32),
                ButtonProfile.NintendoDSOn3DSScaled => ButtonProfile.SNES.ToInputParserBuilder()
                    .Touchscreen(width: 256, height: 192, multitouch: false, allowDrag: true, scaleWidth: 320, scaleHeight: 240),

                ButtonProfile.NES => ButtonProfile.GameBoy.ToInputParserBuilder(),
                ButtonProfile.SNEStoNES => ButtonProfile.NES.ToInputParserBuilder()
                    .AliasedButtons(("a", "b"),
                        ("b", "y")), // SNES B and Y map to NES A and B. SNES X and A do nothing.
                ButtonProfile.SNES => ButtonProfile.GameBoyAdvance.ToInputParserBuilder()
                    .Buttons("x", "y"),
                ButtonProfile.N64 => InputParserBuilder.FromBare()
                    .Buttons("a", "b", "start", "l", "r", "z")
                    .DPad()
                    .SimpleAliasedDPad("d", "")
                    .DPad("c")
                    .AnalogStick("a", true)
                    .SimpleAliasedAnalogStick("l", "a", true)
                    .LengthRestrictions(maxSetLength: 4, maxSequenceLength: 1),
                ButtonProfile.GameCube => InputParserBuilder.FromBare()
                    .Buttons("a", "b", "x", "y", "z", "start")
                    .AnalogInputs("l", "r")
                    .AliasedButtons(("pause", "start"))
                    .DPad()
                    .SimpleAliasedDPad("d", "")
                    .AnalogStick("l", true)
                    .AnalogStick("r", true)
                    .SimpleAliasedAnalogStick("c", "r", true)
                    .Conflicts(("x", "start")) // Prevent Soft Reset in Pokemon XD (B+X+Start)
                    .LengthRestrictions(maxSetLength: 4, maxSequenceLength: 1),
                ButtonProfile.Switch => InputParserBuilder.FromBare()
                    .Buttons("a", "b", "x", "y", "l", "r", "zl", "zr", "lstick", "rstick", "plus", "minus") // Capture and Home buttons omitted on purpose
                    .AliasedButtons(("start", "plus"), ("select", "minus"), ("+", "plus"), ("-", "minus"), ("l2", "zl"), ("r2", "zr"), ("l3", "lstick"), ("r3", "rstick"))
                    .DPad()
                    .SimpleAliasedDPad("d", "")
                    .AnalogStick("l", true)
                    .AnalogStick("r", true)
                    .SimpleAliasedAnalogStick("c", "r", true)
                    .LengthRestrictions(maxSetLength: 4, maxSequenceLength: 1),

                ButtonProfile.SuperGameBoy => ButtonProfile.GameBoy.ToInputParserBuilder(),
                ButtonProfile.GameBoyTower => ButtonProfile.GameBoy.ToInputParserBuilder()
                    .AliasedButtons(("select", "l")),
                ButtonProfile.GameBoyPlayer => ButtonProfile.GameBoyAdvance.ToInputParserBuilder()
                    .AliasedButtons(("select", "y")),

                ButtonProfile.DualGameBoy => ButtonProfile.GameBoy.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
                ButtonProfile.DualNES => ButtonProfile.NES.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
                ButtonProfile.DualSNEStoNES => ButtonProfile.SNEStoNES.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
                ButtonProfile.DualSNES => ButtonProfile.SNES.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
                ButtonProfile.DualN64 => ButtonProfile.N64.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
                ButtonProfile.DualGameCube => ButtonProfile.GameCube.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
                ButtonProfile.DualNintendoDS => ButtonProfile.NintendoDS.ToInputParserBuilder()
                .LeftRightSidesEnabled(true),
                ButtonProfile.DualNintendo3DS => ButtonProfile.Nintendo3DS.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
                ButtonProfile.DualSwitch => ButtonProfile.Switch.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),

                ButtonProfile.DualNintendoDSOnDSOffset => ButtonProfile.NintendoDSOn3DSOffset.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
                ButtonProfile.DualNintendoDSOnDSScaled => ButtonProfile.NintendoDSOn3DSScaled.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),

                ButtonProfile.DualSuperGameBoy => ButtonProfile.SuperGameBoy.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
                ButtonProfile.DualGameBoyTower => ButtonProfile.GameBoyTower.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
                ButtonProfile.DualGameBoyPlayer => ButtonProfile.GameBoyPlayer.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),

                ButtonProfile.DualSwitchButAlsoIsGB => ButtonProfile.DualSwitch.ToInputParserBuilder()
                    .Buttons("start","select")
                    .LengthRestrictions(maxSetLength: 3, maxSequenceLength: 1),

                ButtonProfile.NEStris => ButtonProfile.SNEStoNES.ToInputParserBuilder()
                    .RemoveInputs("start")
            };
            // sanity check: we enforce the convention that all dual profile must have "dual" in their name.
            if (inputParserBuilder.IsDualRun && !profile.IsDual())
                throw new ArgumentException($"input parser builder resulted in dual inputs, " +
                                            $"but profile does not have 'dual' in its name: {profile}");
            if (!inputParserBuilder.IsDualRun && profile.IsDual())
                throw new ArgumentException($"input parser builder resulted in non-dual inputs, " +
                                            $"but profile has 'dual' in its name: {profile}");
            return inputParserBuilder;
        }
    }
}
