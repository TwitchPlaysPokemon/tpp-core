using System.Collections.Immutable;
using ArgsParsing.TypeParsers;
using ArgsParsing.Types;
using NFluent;
using NodaTime;
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

            Check.ThatCode(async () => await argsParser.Parse<int, int>(ImmutableList.Create("123")))
                .Throws<ArgsParseFailure>()
                .WithMessage("too few arguments");
        }

        [Test]
        public void TestTooManyArguments()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new IntParser());

            Check.ThatCode(async () => await argsParser.Parse<int>(ImmutableList.Create("123", "234")))
                .Throws<ArgsParseFailure>()
                .WithMessage("too many arguments");
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

            Check.ThatCode(async () => await argsParser.Parse<Optional<int>>(ImmutableList.Create("abc")))
                .Throws<ArgsParseFailure>()
                .WithMessage("did not recognize 'abc' as a number");
        }

        /// <summary>
        /// Tests that a relevant error message that was wrapped inside a successful result (an Optional), but is then
        /// wrapped again in a failure (an AnyOrder), all error messages should be preserved
        /// and the relevant ones properly displayed.
        /// </summary>
        [Test]
        public void TestErrorMessageFromDeeplyNestedFailure()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new IntParser());
            argsParser.AddArgumentParser(new InstantParser());
            argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
            argsParser.AddArgumentParser(new OptionalParser(argsParser));

            Check.ThatCode(async () =>
                    await argsParser.Parse<AnyOrder<Optional<int>, Optional<Instant>>>(ImmutableList.Create("X", "Y")))
                .Throws<ArgsParseFailure>()
                .WithMessage("did not recognize 'X' as a number, or did not recognize 'X' as a UTC-instant")
                .And.WhichMember(ex => ex.Failures)
                .ContainsExactly(
                    new Failure(ErrorRelevanceConfidence.Default, "did not recognize 'X' as a number"),
                    new Failure(ErrorRelevanceConfidence.Default, "did not recognize 'X' as a UTC-instant"),
                    new Failure(ErrorRelevanceConfidence.Unlikely, "too many arguments")
                );
        }
    }
}
