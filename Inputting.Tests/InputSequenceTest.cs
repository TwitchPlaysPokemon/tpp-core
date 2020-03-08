using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Inputting.Tests
{
    public class InputSequenceTest
    {
        private static InputSet Set(params string[] inputs) =>
            new InputSet(inputs.Select(s => new Input(s, s, s, true)));

        private static InputSequence Seq(params InputSet[] inputSets) => new InputSequence(inputSets);

        [Test]
        public void TestEffectivelyEqualsAnyOrder()
        {
            Assert.AreEqual(Set("a", "b"), Set("a", "b"));
            Assert.IsTrue(Set("a", "b").EqualsEffectively(Set("b", "a")));

            Assert.AreNotEqual(Set("a", "b"), Set("b", "a"));
            Assert.IsFalse(Set("a", "b").EqualsEffectively(Set("a", "x")));

            Assert.AreEqual(Seq(Set("a", "b"), Set("c", "d")), Seq(Set("a", "b"), Set("c", "d")));
            Assert.IsTrue(Seq(Set("a", "b"), Set("c", "d")).EqualsEffectively(Seq(Set("b", "a"), Set("d", "c"))));

            Assert.AreNotEqual(Seq(Set("a", "b"), Set("c", "d")), Seq(Set("b", "a"), Set("d", "c")));
            Assert.IsFalse(Seq(Set("a", "b"), Set("c", "d")).EqualsEffectively(Seq(Set("a", "b"), Set("c", "x"))));
        }

        [Test]
        public void TestEffectivelyEqualsInputSet()
        {
            var inputRef = new Input("Foo", "a", "foo", true);
            var input1 = new Input("Bar", "a", "bar", true);
            var input2 = new Input("Foo", "a", "foo", false);
            var input3 = new Input("Foo", "b", "foo", true);
            Assert.AreNotEqual(inputRef, input1);
            Assert.AreNotEqual(inputRef, input2);
            Assert.AreNotEqual(inputRef, input3);
            Assert.IsTrue(inputRef.EqualsEffectively(input1));
            Assert.IsFalse(inputRef.EqualsEffectively(input2)); // different data
            Assert.IsFalse(inputRef.EqualsEffectively(input3)); // different effective input
        }

        [Test]
        public void TestEffectivelyEqualsInputSequence()
        {
            var inputRefA = new Input("Foo", "a", "foo", true);
            var inputRefB = new Input("Bar", "b", "bar", false);
            var input1A = new Input("Baz", "b", "baz", false);
            var input1B = new Input("Quz", "a", "quz", true);
            var input2 = new Input("Foo", "a", "foo", true);
            var input3A = new Input("Foo", "a", "foo", true);
            var input3B = new Input("Bar", "b", "bar", true);
            var input4A = new Input("Foo", "a", "foo", true);
            var input4B = new Input("Bar", "a", "bar", false);

            var seqRef = new InputSet(new List<Input> {inputRefA, inputRefB});
            var seq1 = new InputSet(new List<Input> {input1A, input1B});
            var seq2 = new InputSet(new List<Input> {input2});
            var seq3 = new InputSet(new List<Input> {input3A, input3B});
            var seq4 = new InputSet(new List<Input> {input4A, input4B});

            Assert.AreNotEqual(seqRef, seq1);
            Assert.IsTrue(seqRef.EqualsEffectively(seq1));
            Assert.IsFalse(seqRef.EqualsEffectively(seq2)); // different length
            Assert.IsFalse(seqRef.EqualsEffectively(seq3)); // different data
            Assert.IsFalse(seqRef.EqualsEffectively(seq4)); // different effective input
        }
    }
}
