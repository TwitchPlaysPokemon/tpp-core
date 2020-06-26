using System.Collections.Immutable;
using ArgsParsing.TypeParsers;
using ArgsParsing.Types;
using NUnit.Framework;

namespace ArgsParsing.Tests
{
    public class ArgsParserTest
    {
        [Test]
        public void TestTooFewArguments()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new IntParser());

            var ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<int, int>(args: ImmutableList.Create("123")));
            Assert.AreEqual("too few arguments", ex.Message);
        }

        [Test]
        public void TestTooManyArguments()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new IntParser());

            var ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<int>(args: ImmutableList.Create("123", "234")));
            Assert.AreEqual("too many arguments", ex.Message);
        }

        /// <summary>
        /// Tests that the error message with the highest relevance gets displayed,
        /// even if it originated from a parsing that succeeded, that being an empty Optional in this case.
        /// </summary>
        [Test]
        public void TestErrorMessageFromRelevantSuccess()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new IntParser());
            argsParser.AddArgumentParser(new OptionalParser(argsParser));

            var ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<Optional<int>>(args: ImmutableList.Create("abc")));
            Assert.AreNotEqual("too many arguments", ex.Message);
            Assert.AreEqual("did not recognize 'abc' as a number", ex.Message);
        }

        /// <summary>
        /// Tests that a relevant error message that was wrapped inside a successful result (an Optional), but is then
        /// wrapped again in a failure (an AnyOrder), the nested error message should be displayed and not be lost.
        /// </summary>
        [Test]
        public void TestErrorMessageFromDeeplyNestedFailure()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new IntParser());
            argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
            argsParser.AddArgumentParser(new OptionalParser(argsParser));

            var ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<AnyOrder<Optional<int>, Optional<int>>>(args: ImmutableList.Create("abc", "123")));
            Assert.AreNotEqual("too many arguments", ex.Message);
            Assert.AreEqual("did not recognize 'abc' as a number", ex.Message);
        }
    }
}
