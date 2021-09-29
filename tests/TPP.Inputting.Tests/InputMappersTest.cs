using System.Collections.Generic;
using NUnit.Framework;
using TPP.Inputting.Parsing;

namespace TPP.Inputting.Tests;

public class InputMappersTest
{
    private static readonly IInputParser InputParser = InputParserBuilder.FromBare()
        .Buttons("A", "B", "X", "Y", "start", "select")
        .HoldEnabled(true)
        .LengthRestrictions(maxSetLength: 4, maxSequenceLength: 1)
        .Touchscreen(800, 600, multitouch: true, allowDrag: true)
        .Build();

    private static InputSet ParseInput(string inputStr)
    {
        InputSequence? inputSequence = InputParser.Parse(inputStr);
        Assert.NotNull(inputSequence);
        Assert.AreEqual(1, inputSequence!.InputSets.Count);
        return inputSequence.InputSets[0];
    }

    [Test]
    public void proper_representation()
    {
        IInputMapper inputMapper = new DefaultTppInputMapper();
        var expectedInputMap = new Dictionary<string, object>
        {
            ["Touch_Screen_X"] = 10,
            ["Touch_Screen_Y"] = 20,
            ["Touch_Screen_X2"] = 30,
            ["Touch_Screen_Y2"] = 40,
            ["A"] = true,
            ["B"] = true,
            ["Start"] = true,
            ["Held_Frames"] = 60,
            ["Sleep_Frames"] = 120,
        };
        IDictionary<string, object> producedInputMap = inputMapper.Map(
            new TimedInputSet(ParseInput("10,20>30,40+A+b+start"), 1, 2));
        Assert.AreEqual(expectedInputMap, producedInputMap);
    }
}
