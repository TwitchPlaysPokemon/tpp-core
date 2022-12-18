using System.Runtime.Serialization;
using TPP.Inputting;

namespace TPP.Core
{
    [DataContract]
    public enum ButtonProfile
    {
        [EnumMember(Value = "gb")] GameBoy,
        [EnumMember(Value = "gba")] GameBoyAdvance,
        [EnumMember(Value = "dualgb")] DualGameBoy,
    }

    public static class ButtonProfileExtensions
    {
        public static InputParserBuilder ToInputParserBuilder(this ButtonProfile profile) =>
            profile switch
            {
                ButtonProfile.GameBoy => InputParserBuilder.FromBare()
                    .Buttons("a", "b", "start", "select")
                    .DPad()
                    .RemappedDPad(up: "n", down: "s", left: "w", right: "e", mapsToPrefix: "")
                    .RemappedDPad(up: "north", down: "south", left: "west", right: "east", mapsToPrefix: "")
                    .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 1)
                    .HoldEnabled(true),
                ButtonProfile.GameBoyAdvance => ButtonProfile.GameBoy.ToInputParserBuilder()
                    .Buttons("l", "r"),
                ButtonProfile.DualGameBoy => ButtonProfile.GameBoy.ToInputParserBuilder()
                    .LeftRightSidesEnabled(true),
            };
    }
}
