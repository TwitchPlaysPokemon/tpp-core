using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Inputting.InputDefinitions;
using Inputting.Parsing;
using NUnit.Framework;

namespace Inputting.Tests.Parsing
{
    public class BareInputParserTest
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
        public void TestBasicInputs()
        {
            _inputParser = new BareInputParser(
                inputDefinitions: new List<string> {"a", "b", "start", "select"}
                    .Select(s => (IInputDefinition) new ButtonInputDefinition(s)),
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            );

            // good cases
            AssertInput("A", Seq(Set("a")));
            AssertInput("aa", Seq(Set("a"), Set("a")));
            AssertInput("a2a", Seq(Set("a"), Set("a"), Set("a")));
            AssertInput("a2a2", Seq(Set("a"), Set("a"), Set("a"), Set("a")));
            AssertInput("start4", Seq(Set("start"), Set("start"), Set("start"), Set("start")));
            AssertInput("a+b2startSelect", Seq(Set("a", "b"), Set("a", "b"), Set("start"), Set("select")));
            AssertInput("b+a", Seq(Set("b", "a"))); // order is preserved

            // bad cases
            AssertInput("x", null); // not a button
            AssertInput("a+b+start", null); // set too long
            AssertInput("a5", null); // sequence too long
        }

        [Test]
        public void TestHold()
        {
            // hold enabled
            _inputParser = new BareInputParser(
                inputDefinitions: new List<string> {"a"}
                    .Select(s => (IInputDefinition) new ButtonInputDefinition(s)),
                maxSetLength: 2, maxSequenceLength: 1,
                holdEnabled: true
            );

            AssertInput("a", Seq(Set("a")));
            Assert.AreEqual(Seq(new InputSet(new List<Input>
                {new Input("a", "a", "a", true), new Input("hold", "hold", "-", true)})
            ), _inputParser.Parse("a-"));

            // hold disabled
            _inputParser = new BareInputParser(
                inputDefinitions: new List<string> {"a"}
                    .Select(s => (IInputDefinition) new ButtonInputDefinition(s)),
                maxSetLength: 2, maxSequenceLength: 1,
                holdEnabled: false
            );

            AssertInput("a", Seq(Set("a")));
            AssertInput("a-", null);
        }

        [Test]
        public void TestAlias()
        {
            _inputParser = new BareInputParser(
                inputDefinitions: new List<IInputDefinition>
                {
                    new ButtonInputDefinition("y"),
                    new AliasedButtonInputDefinition("honk", "y"),
                },
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            );

            var result = _inputParser.Parse("honky");
            Assert.AreEqual(Seq(
                new InputSet(new List<Input> {new Input("honk", "y", "honk", true)}),
                new InputSet(new List<Input> {new Input("y", "y", "y", true)})
            ), result);
        }

        [Test]
        public void TestRemapping()
        {
            _inputParser = new BareInputParser(
                inputDefinitions: new List<IInputDefinition>
                {
                    new ButtonInputDefinition("y"),
                    new RemappedButtonInputDefinition("honk", "y"),
                },
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            );

            var result = _inputParser.Parse("honky");
            Assert.AreEqual(Seq(
                new InputSet(new List<Input> {new Input("y", "y", "honk", true)}),
                new InputSet(new List<Input> {new Input("y", "y", "y", true)})
            ), result);
        }

        [Test]
        public void TestTouchscreen()
        {
            _inputParser = new BareInputParser(
                inputDefinitions: new List<IInputDefinition>
                {
                    new ButtonInputDefinition("a"),
                    new TouchscreenInputDefinition(width: 240, height: 160),
                },
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            );

            Assert.AreEqual(Seq(
                new InputSet(new List<Input> {new Input("a", "a", "a", true)}),
                new InputSet(new List<Input>
                {
                    new Input("234,123", "touch", "234,123", additionalData: (234, 123))
                }),
                new InputSet(new List<Input> {new Input("a", "a", "a", true)})
            ), _inputParser.Parse("a234,123a"));
            Assert.AreEqual(Seq(
                new InputSet(new List<Input>
                {
                    new Input("239,159", "touch", "239,159", additionalData: (239, 159))
                })
            ), _inputParser.Parse("239,159"));
            Assert.AreEqual(null, _inputParser.Parse("240,159"));
            Assert.AreEqual(null, _inputParser.Parse("239,160"));
        }

        [Test]
        public void TestTouchscreenDrag()
        {
            _inputParser = new BareInputParser(
                inputDefinitions: new List<IInputDefinition>
                {
                    new ButtonInputDefinition("a"),
                    new TouchscreenDragInputDefinition(width: 240, height: 160),
                },
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            );

            Assert.AreEqual(Seq(
                new InputSet(new List<Input> {new Input("a", "a", "a", true)}),
                new InputSet(new List<Input>
                {
                    new Input("234,123>222,111", "drag", "234,123>222,111", additionalData: (234, 123, 222, 111))
                }),
                new InputSet(new List<Input> {new Input("a", "a", "a", true)})
            ), _inputParser.Parse("a234,123>222,111a"));
            Assert.AreEqual(Seq(
                new InputSet(new List<Input>
                {
                    new Input("239,159>0,0", "drag", "239,159>0,0", additionalData: (239, 159, 0, 0))
                })
            ), _inputParser.Parse("239,159>0,0"));
            Assert.AreEqual(null, _inputParser.Parse("240,159>0,0"));
            Assert.AreEqual(null, _inputParser.Parse("239,160>0,0"));
            Assert.AreEqual(null, _inputParser.Parse("0,0>239,160"));
            Assert.AreEqual(null, _inputParser.Parse("0,0>239,160"));
        }

        [Test]
        public void TestAnalog()
        {
            _inputParser = new BareInputParser(
                inputDefinitions: new List<IInputDefinition>
                {
                    new ButtonInputDefinition("a"),
                    new AnalogInputDefinition("up"),
                },
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            );

            Assert.AreEqual(Seq(
                new InputSet(new List<Input> {new Input("a", "a", "a", true)}),
                new InputSet(new List<Input> {new Input("up", "up", "up", 0.2f)}),
                new InputSet(new List<Input> {new Input("up", "up", "up", 0.2f)}),
                new InputSet(new List<Input> {new Input("a", "a", "a", true)})
            ), _inputParser.Parse("aup.22a"));
            Assert.AreEqual(Seq(
                new InputSet(new List<Input> {new Input("up", "up", "up", 1.0f)})
            ), _inputParser.Parse("up"));
            Assert.AreEqual(Seq(
                new InputSet(new List<Input> {new Input("up", "up", "up", 0.9f)})
            ), _inputParser.Parse("up.9"));
            Assert.AreEqual(null, _inputParser.Parse("up."));
            Assert.AreEqual(null, _inputParser.Parse("up.0"));
            Assert.AreEqual(null, _inputParser.Parse("up.10"));
        }

        [Test]
        public void TestPerformance()
        {
            var inputDefinitions = new List<IInputDefinition>
            {
                new ButtonInputDefinition("a"),
                new ButtonInputDefinition("b"),
                new ButtonInputDefinition("start"),
                new ButtonInputDefinition("select"),
                new ButtonInputDefinition("up"),
                new ButtonInputDefinition("down"),
                new ButtonInputDefinition("left"),
                new ButtonInputDefinition("right"),
                new RemappedButtonInputDefinition("n", "up"),
                new RemappedButtonInputDefinition("s", "down"),
                new RemappedButtonInputDefinition("w", "left"),
                new RemappedButtonInputDefinition("e", "right"),
                new ButtonInputDefinition("wait"),
                new RemappedButtonInputDefinition("p", "wait"),
                new RemappedButtonInputDefinition("xp", "wait"),
                new RemappedButtonInputDefinition("exp", "wait"),
                new AliasedButtonInputDefinition("honk", "y"),
                new TouchscreenInputDefinition(width: 240, height: 160),
                new TouchscreenDragInputDefinition(width: 240, height: 160),
                new AnalogInputDefinition("cup"),
                new AnalogInputDefinition("cdown"),
                new AnalogInputDefinition("cleft"),
                new AnalogInputDefinition("cright"),
            };
            _inputParser = new BareInputParser(
                inputDefinitions: inputDefinitions,
                maxSetLength: 2, maxSequenceLength: 4, holdEnabled: true
            );

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                _inputParser.Parse("a");
                _inputParser.Parse("A");
                _inputParser.Parse("aa");
                _inputParser.Parse("123,234");
                _inputParser.Parse("11,22>33,44");
                _inputParser.Parse("a+b2startSelect");
                _inputParser.Parse("cupupcup.2up");
                _inputParser.Parse("p");
                _inputParser.Parse("phonk+left-");
                _inputParser.Parse("start9");
            }
            stopwatch.Stop();
            // check for 1k inputs per second. I get up to 50k inputs per second,
            // but this test should only fail if something got horribly slow.
            Assert.Less(stopwatch.Elapsed, TimeSpan.FromSeconds(1));
        }
    }
}
