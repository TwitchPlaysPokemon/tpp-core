using System;
using System.Runtime.Serialization;
using TPP.Inputting;
using TPP.Inputting.Parsing;

namespace TPP.Core;

[DataContract]
public enum ButtonProfile
{
    [EnumMember(Value = "gb")] GameBoy,
}

public static class ButtonProfileExtensions
{
    public static IInputParser ToInputParser(this ButtonProfile profile) =>
        profile switch
        {
            ButtonProfile.GameBoy => InputParserBuilder.FromBare()
                .Buttons("a", "b", "start", "select", "wait")
                .DPad()
                .RemappedDPad(up: "n", down: "s", left: "w", right: "e", mapsToPrefix: "")
                .RemappedButtons(("p", "wait"), ("xp", "wait"), ("exp", "wait"))
                .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 1)
                .HoldEnabled(true)
                .Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "missing input parser definition")
        };
}
