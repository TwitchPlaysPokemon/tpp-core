using System.Collections.Generic;
using NUnit.Framework;
using TPP.Inputting.Parsing;

namespace TPP.Inputting.Tests
{
    public class InputMappersTest
    {
        private static readonly IInputParser InputParser = InputParserBuilder.FromBare()
            .Buttons("A", "B", "X", "Y", "start", "select")
            .AnalogStick("l", true)
            .HoldEnabled(true)
            .LengthRestrictions(maxSetLength: 4, maxSequenceLength: 1)
            .Touchscreen(800, 600, multitouch: true, allowDrag: true)
            .Build();

        private static InputSet ParseInput(string inputStr)
        {
            InputSequence? inputSequence = InputParser.Parse(inputStr);
            Assert.That(inputSequence, Is.Not.Null);
            Assert.That(inputSequence!.InputSets, Has.Count.EqualTo(1));
            return inputSequence.InputSets[0];
        }

        [Test]
        public void ProperRepresentation()
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
            Assert.That(expectedInputMap, Is.EqualTo(producedInputMap));
        }

        [Test]
        public void AnalogInputs()
        {
            IInputMapper inputMapper = new DefaultTppInputMapper();
            var expectedInputMap = new Dictionary<string, object>
            {
                ["Lup"] = 0.5f,
                ["Held_Frames"] = 60,
                ["Sleep_Frames"] = 120,
            };
            IDictionary<string, object> producedInputMap = inputMapper.Map(
                new TimedInputSet(ParseInput("ln.5"), 1, 2));
            Assert.That(expectedInputMap, Is.EqualTo(producedInputMap));
        }
    }
}
