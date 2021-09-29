using System.Collections.Immutable;
using NodaTime;
using NUnit.Framework;
using TPP.ArgsParsing.TypeParsers;
using TPP.ArgsParsing.Types;

namespace TPP.ArgsParsing.Tests;

public class ArgsParserTest
{
    [Test]
    public void TestTooFewArguments()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new NonNegativeIntParser());

        ArgsParseFailure ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<NonNegativeInt, NonNegativeInt>(args: ImmutableList.Create("123")))!;
        Assert.That(ex.Message, Is.EqualTo("too few arguments"));
    }

    [Test]
    public void TestTooManyArguments()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new NonNegativeIntParser());

        ArgsParseFailure ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<NonNegativeInt>(args: ImmutableList.Create("123", "234")))!;
        Assert.That(ex.Message, Is.EqualTo("too many arguments"));
    }

    [Test]
    public void TestMissingParser()
    {
        var argsParser = new ArgsParser();

        MissingParserException ex = Assert.ThrowsAsync<MissingParserException>(() => argsParser
            .Parse<int>(args: ImmutableList.Create("123")))!;
        Assert.That(ex.Message, Is.EqualTo("No parser found for type System.Int32"));
        Assert.That(ex.TypeWithoutParser, Is.EqualTo(typeof(int)));
    }

    /// <summary>
    /// Tests that the error message with the highest relevance gets displayed,
    /// even if it originated from a parsing that succeeded, that being an empty Optional in this case.
    /// </summary>
    [Test]
    public void TestErrorMessageFromRelevantSuccess()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new NonNegativeIntParser());
        argsParser.AddArgumentParser(new OptionalParser(argsParser));

        ArgsParseFailure ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<Optional<NonNegativeInt>>(args: ImmutableList.Create("abc")))!;
        Assert.That(ex.Message, Is.Not.EqualTo("too many arguments"));
        Assert.That(ex.Message, Is.EqualTo("did not recognize 'abc' as a number"));
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
        argsParser.AddArgumentParser(new NonNegativeIntParser());
        argsParser.AddArgumentParser(new InstantParser());
        argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
        argsParser.AddArgumentParser(new OptionalParser(argsParser));

        ArgsParseFailure ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<AnyOrder<Optional<NonNegativeInt>, Optional<Instant>>>(args: ImmutableList.Create("X", "Y")))!;
        Assert.That(ex.Message, Is.Not.EqualTo("too many arguments"));
        Assert.That(ex.Message, Is.EqualTo(
            "did not recognize 'X' as a number, or did not recognize 'X' as a UTC-instant"));
        Assert.That(ex.Failures, Is.EqualTo(new[]
        {
            new Failure(ErrorRelevanceConfidence.Default, "did not recognize 'X' as a number"),
            new Failure(ErrorRelevanceConfidence.Default, "did not recognize 'X' as a UTC-instant"),
            new Failure(ErrorRelevanceConfidence.Unlikely, "too many arguments")
        }));
    }

    /// <summary>
    /// Some argument parsers - the AnyOrderParser in this case - have multiple possible branches.
    /// This means that the same argument may fail to parse on multiple branches.
    /// The resulting duplicated parse failures will be present in the ArgsParseFailure,
    /// but should not be present in the overall parse failure message.
    /// </summary>
    [Test]
    public void TestNoDuplicateErrorMessages()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new NonNegativeIntParser());
        argsParser.AddArgumentParser(new AnyOrderParser(argsParser));

        ArgsParseFailure ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<AnyOrder<NonNegativeInt, NonNegativeInt>>(ImmutableList.Create("1", "x")))!;
        Assert.That(ex.Message, Is.EqualTo("did not recognize 'x' as a number"));
        // this is how it used to be:
        Assert.That(ex.Message, Is.Not.EqualTo(
            "did not recognize 'x' as a number, or did not recognize 'x' as a number"));
    }
}
