using System.Collections.Generic;
using System.Linq;
using Inputting.InputDefinitions;
using Inputting.Parsing;
using NUnit.Framework;

namespace Inputting.Tests.Parsing
{
    public class ContextualInputParserTest
    {
        private static InputSet Set(params string[] inputs) =>
            new InputSet(inputs.Select(s => new Input(s, s, s, true)));

        private static InputSequence Seq(params InputSet[] inputSets) => new InputSequence(inputSets);

        private IInputParser _inputParser = null!;

        private void AssertInput(string rawInput, InputSequence? expectedSequence)
        {
            Assert.AreEqual(expectedSequence, _inputParser.Parse(rawInput));
        }

        [Test]
        public void TestConflicts()
        {
            var conflictingInputs = new HashSet<(string, string)>
            {
                ("up", "down"),
                ("left", "right"),
            };
            _inputParser = new ContextualInputParser(new BareInputParser(
                inputDefinitions: new List<IInputDefinition>
                {
                    new ButtonInputDefinition("a"),
                    new ButtonInputDefinition("up"),
                    new ButtonInputDefinition("down"),
                    new ButtonInputDefinition("left"),
                },
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            ), conflictingInputs, true);

            AssertInput("aupdown", Seq(Set("a"), Set("up"), Set("down")));
            AssertInput("aup+down", null);
            AssertInput("adown+up", null);
            AssertInput("aupleft", Seq(Set("a"), Set("up"), Set("left")));
            AssertInput("aup+left", Seq(Set("a"), Set("up", "left")));
        }

        [Test]
        public void TestMultitouch()
        {
            // multitouch enabled
            _inputParser = new ContextualInputParser(new BareInputParser(
                inputDefinitions: new List<IInputDefinition>
                {
                    new TouchscreenInputDefinition(240, 160),
                    new TouchscreenDragInputDefinition(240, 160),
                },
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            ), new List<(string, string)>(), multitouch: true);

            Assert.AreEqual(Seq(new InputSet(new List<Input>
                {
                    new Input("234,123", "touch", "234,123", (234, 123)),
                    new Input("11,22", "touch", "11,22", (11, 22)),
                })
            ), _inputParser.Parse("234,123+11,22"));
            Assert.AreEqual(Seq(new InputSet(new List<Input>
                {
                    new Input("234,123", "touch", "234,123", (234, 123)),
                    new Input("11,22>33,44", "drag", "11,22>33,44", (11, 22, 33, 44)),
                })
            ), _inputParser.Parse("234,123+11,22>33,44"));

            // multitouch disabled
            _inputParser = new ContextualInputParser(new BareInputParser(
                inputDefinitions: new List<IInputDefinition>
                {
                    new TouchscreenInputDefinition(240, 160),
                    new TouchscreenDragInputDefinition(240, 160),
                },
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            ), new List<(string, string)>(), multitouch: false);

            Assert.IsNull(_inputParser.Parse("234,123+11,22"));
            Assert.IsNull(_inputParser.Parse("234,123+11,22>33,44"));
            Assert.IsNull(_inputParser.Parse("234,123>0,0+11,22>33,44"));
        }

        [Test]
        public void TestRejectDuplicates()
        {
            _inputParser = new ContextualInputParser(new BareInputParser(
                inputDefinitions: new List<IInputDefinition>
                {
                    new ButtonInputDefinition("a"),
                    new AnalogInputDefinition("up"),
                    new AliasedButtonInputDefinition("n", "up"),
                    new TouchscreenInputDefinition(240, 160),
                    new TouchscreenDragInputDefinition(240, 160),
                },
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            ), new List<(string, string)>(), true);

            Assert.IsNull(_inputParser.Parse("a+a"));
            Assert.IsNull(_inputParser.Parse("up+up.1"));
            Assert.IsNull(_inputParser.Parse("n+up"));
            Assert.IsNull(_inputParser.Parse("11,22+11,22"));
            Assert.IsNull(_inputParser.Parse("11,22>50,60+11,22>50,60"));
        }
    }
}
