using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Moq;
using NodaTime;
using NodaTime.Text;
using NUnit.Framework;
using TPP.ArgsParsing.TypeParsers;
using TPP.ArgsParsing.Types;
using TPP.Common;
using TPP.Model;
using TPP.Persistence;

namespace TPP.ArgsParsing.Tests;

public class TypeParsersTest
{
    [Test]
    public async Task TestAnyOrderParser()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
        argsParser.AddArgumentParser(new StringParser());
        argsParser.AddArgumentParser(new SignedIntParser());

        var args1 = ImmutableList.Create("123", "foo");
        var args2 = ImmutableList.Create("foo", "123");
        (SignedInt int1, string string1) = await argsParser.Parse<AnyOrder<SignedInt, string>>(args1);
        (SignedInt int2, string string2) = await argsParser.Parse<AnyOrder<SignedInt, string>>(args2);
        Assert.That((int)int1, Is.EqualTo(123));
        Assert.That((int)int2, Is.EqualTo(123));
        Assert.That(string1, Is.EqualTo("foo"));
        Assert.That(string2, Is.EqualTo("foo"));

        ArgsParseFailure ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<AnyOrder<SignedInt, string>>(ImmutableList.Create("foo", "bar")))!;
        Assert.That(ex.Failures.Count, Is.EqualTo(2));
        Assert.That(ex.Message,
            Is.EqualTo("did not recognize 'foo' as a number, or did not recognize 'bar' as a number"));
    }

    [Test]
    public async Task TestAnyOrderOptional()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
        argsParser.AddArgumentParser(new OptionalParser(argsParser));
        argsParser.AddArgumentParser(new SignedIntParser());
        argsParser.AddArgumentParser(new StringParser());

        // this used to cause a stack overflow in the any order parser
        (Optional<SignedInt> optionalInt, Optional<string> optionalString) = await argsParser
            .Parse<AnyOrder<Optional<SignedInt>, Optional<string>>>(ImmutableList<string>.Empty);
        Assert.False(optionalInt.IsPresent);
        Assert.False(optionalString.IsPresent);
    }

    [Test]
    public async Task TestAnyOrderLongestFirst()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
        argsParser.AddArgumentParser(new OptionalParser(argsParser));
        argsParser.AddArgumentParser(new SignedIntParser());
        argsParser.AddArgumentParser(new StringParser());

        // this used to fail because the optional made the first permutation fit with remaining arguments,
        // instead of continuing to try all permutations and return the one consuming the most arguments.
        (Optional<SignedInt> optionalInt, string str) = await argsParser
            .Parse<AnyOrder<Optional<SignedInt>, string>>(ImmutableList.Create("abc", "123"));
        Assert.True(optionalInt.IsPresent);
        Assert.That((int)optionalInt.Value, Is.EqualTo(123));
        Assert.That(str, Is.EqualTo("abc"));
    }

    [Test]
    public async Task TestAnyOrderSpareArgs()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
        argsParser.AddArgumentParser(new SignedIntParser());
        argsParser.AddArgumentParser(new StringParser());

        ((SignedInt num1, string str), SignedInt num2) = await argsParser
            .Parse<AnyOrder<SignedInt, string>, SignedInt>(ImmutableList.Create("1", "a", "2"));
        Assert.That((int)num1, Is.EqualTo(1));
        Assert.That(str, Is.EqualTo("a"));
        Assert.That((int)num2, Is.EqualTo(2));
    }

    [Test]
    public async Task TestAnyOrderTypeConsumingMultipleArgs()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
        argsParser.AddArgumentParser(new SignedIntParser());
        argsParser.AddArgumentParser(new InstantParser());

        (SignedInt num, Instant instant) = await argsParser
            .Parse<AnyOrder<SignedInt, Instant>>(ImmutableList.Create("1", "2000-01-01", "12:00:00Z"));
        Assert.That((int)num, Is.EqualTo(1));
        Assert.That(instant, Is.EqualTo(Instant.FromUtc(2000, 1, 1, 12, 0)));
    }

    [Test]
    public async Task TestBoolParser()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new BoolParser());

        Assert.That(await argsParser.Parse<bool>(ImmutableList.Create("True")), Is.True);
        Assert.That(await argsParser.Parse<bool>(ImmutableList.Create("YES")), Is.True);
        Assert.That(await argsParser.Parse<bool>(ImmutableList.Create("false")), Is.False);
        Assert.That(await argsParser.Parse<bool>(ImmutableList.Create("nO")), Is.False);

        ArgsParseFailure failure = Assert.ThrowsAsync<ArgsParseFailure>(async () =>
            await argsParser.Parse<bool>(ImmutableList.Create("?")))!;
        Assert.That(failure.Message, Is.EqualTo("Did not recognize '?' as a boolean"));
    }

    [Test]
    public async Task TestInstantParser()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new InstantParser());

        var result1 = await argsParser.Parse<Instant>(args: ImmutableList.Create("2020-03-22", "01:59:20Z"));
        var result2 = await argsParser.Parse<Instant>(args: ImmutableList.Create("2020-03-22T01:59:20Z"));

        Instant refInstant = InstantPattern.General.Parse("2020-03-22T01:59:20Z").Value;
        Assert.That(result1, Is.EqualTo(refInstant));
        Assert.That(result2, Is.EqualTo(refInstant));
        Assert.That(result2, Is.EqualTo(result1));

        ArgsParseFailure ex1 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<Instant>(ImmutableList.Create("2020-03-22T01:59:20+02")))!;
        ArgsParseFailure ex2 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<Instant>(ImmutableList.Create("asdasdasd")))!;
        Assert.That(ex1.Message, Is.EqualTo("did not recognize '2020-03-22T01:59:20+02' as a UTC-instant"));
        Assert.That(ex2.Message, Is.EqualTo("did not recognize 'asdasdasd' as a UTC-instant"));
    }

    [Test]
    public async Task TestHexColorParser()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new HexColorParser());

        Assert.That((await argsParser
            .Parse<HexColor>(args: ImmutableList.Create("#abcdef"))).StringWithHash, Is.EqualTo("#ABCDEF"));
        Assert.That((await argsParser
            .Parse<HexColor>(args: ImmutableList.Create("abcdef"))).StringWithHash, Is.EqualTo("#ABCDEF"));
        Assert.That((await argsParser
            .Parse<HexColor>(args: ImmutableList.Create("#0f9D5a"))).StringWithHash, Is.EqualTo("#0F9D5A"));

        ArgsParseFailure ex1 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<HexColor>(ImmutableList.Create("blabla")))!;
        ArgsParseFailure ex2 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<HexColor>(ImmutableList.Create("#abc")))!;
        ArgsParseFailure ex3 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<HexColor>(ImmutableList.Create("#bcdefg")))!;
        Assert.That(ex1.Message, Is.EqualTo("'blabla' is not a valid hex color"));
        Assert.That(ex2.Message, Is.EqualTo("'#abc' must be a 6-character hex code consisting of 0-9 and A-F, " +
                                            "for example '#FF0000' for pure red."));
        Assert.That(ex3.Message, Is.EqualTo("'#bcdefg' must be a 6-character hex code consisting of 0-9 and A-F, " +
                                            "for example '#FF0000' for pure red."));
    }

    [Test]
    public async Task TestOneOfParser()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new OneOfParser(argsParser));
        argsParser.AddArgumentParser(new StringParser());
        argsParser.AddArgumentParser(new SignedIntParser());
        argsParser.AddArgumentParser(new InstantParser());

        OneOf<SignedInt, string> result1 =
            await argsParser.Parse<OneOf<SignedInt, string>>(ImmutableList.Create("123"));
        OneOf<SignedInt, string> result2 =
            await argsParser.Parse<OneOf<SignedInt, string>>(ImmutableList.Create("foo"));
        Assert.IsTrue(result1.Item1.IsPresent);
        Assert.IsFalse(result1.Item2.IsPresent);
        Assert.That((int)result1.Item1.Value, Is.EqualTo(123));
        Assert.IsFalse(result2.Item1.IsPresent);
        Assert.IsTrue(result2.Item2.IsPresent);
        Assert.That(result2.Item2.Value, Is.EqualTo("foo"));

        ArgsParseFailure exUnrecognized = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<OneOf<SignedInt, Instant>>(ImmutableList.Create("foo")))!;
        Assert.That(exUnrecognized.Failures.Count, Is.EqualTo(2));
        const string errorText = "did not recognize 'foo' as a number, or did not recognize 'foo' as a UTC-instant";
        Assert.That(exUnrecognized.Message, Is.EqualTo(errorText));
        ArgsParseFailure exTooManyArgs = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<OneOf<SignedInt, SignedInt>>(ImmutableList.Create("123", "234")))!;
        Assert.That(exTooManyArgs.Message, Is.EqualTo("too many arguments"));
    }

    [Test]
    public async Task TestOptionalParser()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new OptionalParser(argsParser));
        argsParser.AddArgumentParser(new StringParser());
        argsParser.AddArgumentParser(new SignedIntParser());

        var result1 = await argsParser.Parse<Optional<SignedInt>>(args: ImmutableList.Create("123"));
        var result2 = await argsParser.Parse<Optional<SignedInt>>(args: ImmutableList<string>.Empty);
        (Optional<SignedInt> result3, string _) = await argsParser
            .Parse<Optional<SignedInt>, string>(args: ImmutableList.Create("foo"));
        Assert.IsTrue(result1.IsPresent);
        Assert.That((int)result1.Value, Is.EqualTo(123));
        Assert.IsFalse(result2.IsPresent);
        Assert.IsFalse(result3.IsPresent);
    }

    [Test]
    public async Task TestPercentageParser()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new PercentageParser());

        Percentage percentageOver100 = await argsParser.Parse<Percentage>(ImmutableList.Create("1234.5678%"));
        Percentage percentageExactly100 = await argsParser.Parse<Percentage>(ImmutableList.Create("100%"));
        Percentage percentageZero = await argsParser.Parse<Percentage>(ImmutableList.Create("0%"));
        Assert.That(percentageOver100.AsPercent, Is.EqualTo(1234.5678));
        Assert.That(percentageOver100.AsDecimal, Is.EqualTo(12.345678));
        Assert.That(percentageExactly100.AsPercent, Is.EqualTo(100.0));
        Assert.That(percentageExactly100.AsDecimal, Is.EqualTo(1.0));
        Assert.That(percentageZero.AsPercent, Is.EqualTo(0.0));
        Assert.That(percentageZero.AsDecimal, Is.EqualTo(0.0));

        ArgsParseFailure failureNoNumber = Assert.ThrowsAsync<ArgsParseFailure>(() =>
            argsParser.Parse<Percentage>(ImmutableList.Create("abc%")))!;
        ArgsParseFailure failureNoPercentSign = Assert.ThrowsAsync<ArgsParseFailure>(() =>
            argsParser.Parse<Percentage>(ImmutableList.Create("1.23")))!;
        ArgsParseFailure failureNegative = Assert.ThrowsAsync<ArgsParseFailure>(() =>
            argsParser.Parse<Percentage>(ImmutableList.Create("-5%")))!;
        Assert.That(failureNoNumber.Message, Is.EqualTo("did not recognize 'abc' as a decimal"));
        Assert.That(failureNoPercentSign.Message, Is.EqualTo("percentages must end in '%'"));
        Assert.That(failureNegative.Message, Is.EqualTo("percentage cannot be negative"));
    }

    [Test]
    public async Task TestPkmnSpeciesParser()
    {
        const string speciesId = "79317";
        const string speciesName = "Uniquamon";
        var argsParser = new ArgsParser();
        PkmnSpecies species = PkmnSpecies.RegisterName(speciesId, speciesName);
        argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));

        PkmnSpecies resultById = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("#" + speciesId));
        PkmnSpecies resultByPaddedId =
            await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("#0" + speciesId));
        PkmnSpecies resultByName1 = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create(speciesName));
        PkmnSpecies resultByName2 = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("uNiQuAmOn"));

        Assert.That(resultById, Is.EqualTo(species));
        Assert.That(resultByPaddedId, Is.EqualTo(species));
        Assert.That(resultByName1, Is.EqualTo(species));
        Assert.That(resultByName2, Is.EqualTo(species));

        ArgsParseFailure exNotPrefixed = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<PkmnSpecies>(args: ImmutableList.Create(speciesId)))!;
        Assert.That(exNotPrefixed.Message, Is.EqualTo("Please prefix with '#' to supply and pokedex number"));
        ArgsParseFailure exUnknown = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<PkmnSpecies>(args: ImmutableList.Create("unknown")))!;
        Assert.That(exUnknown.Message, Is.EqualTo(
            "No pokemon with the name 'unknown' was recognized. Please supply a valid name, " +
            "or prefix with '#' to supply and pokedex number instead"));
    }

    [Test]
    public async Task TestPkmnSpeciesParserNameNormalization()
    {
        var argsParser = new ArgsParser();
        PkmnSpecies species = PkmnSpecies.RegisterName("123", "'Mahina: -Pea.");
        Regex removeCharsRegex = new Regex("[ '-.:]");
        string NormalizeName(string name) => removeCharsRegex.Replace(name, "");
        argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }, NormalizeName));

        PkmnSpecies resultById = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("#123"));
        PkmnSpecies result1 = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("mahina", "pea"));
        PkmnSpecies result2 = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("mahina:", "-pea"));
        PkmnSpecies result3 = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("MAHINA:pea"));
        PkmnSpecies result4 = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("mahinaPEA"));
        PkmnSpecies result5 = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("'mahina-pea."));

        Assert.That(resultById, Is.EqualTo(species));
        Assert.That(result1, Is.EqualTo(species));
        Assert.That(result2, Is.EqualTo(species));
        Assert.That(result3, Is.EqualTo(species));
        Assert.That(result4, Is.EqualTo(species));
        Assert.That(result5, Is.EqualTo(species));

        ArgsParseFailure exNotRecognized = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<PkmnSpecies>(args: ImmutableList.Create("mahina", "aaaaaa")))!;
        Assert.That(exNotRecognized.Message, Is.EqualTo(
            "No pokemon with the name 'mahina' was recognized. Please supply a valid name, " +
            "or prefix with '#' to supply and pokedex number instead"));
        ArgsParseFailure exNoAccidentalHashRemoval = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<PkmnSpecies>(args: ImmutableList.Create("#mahinapea")))!;
        Assert.That(exNoAccidentalHashRemoval.Message, Is.EqualTo("did not recognize species '#mahinapea'"));
    }

    [TestFixture]
    private class PrefixedNumberParser
    {
        private readonly ArgsParser _argsParser;

        public PrefixedNumberParser()
        {
            _argsParser = new ArgsParser();
            _argsParser.AddArgumentParser(new PokeyenParser());
            _argsParser.AddArgumentParser(new TokensParser());
            _argsParser.AddArgumentParser(new SignedPokeyenParser());
            _argsParser.AddArgumentParser(new SignedTokensParser());
        }

        [Test]
        public async Task accepts_normal_numbers()
        {
            Assert.That((int)await _argsParser.Parse<Pokeyen>(ImmutableList.Create("P11")), Is.EqualTo(11));
            Assert.That((int)await _argsParser.Parse<Tokens>(ImmutableList.Create("T22")), Is.EqualTo(22));
            Assert.That((int)await _argsParser.Parse<SignedPokeyen>(ImmutableList.Create("P33")), Is.EqualTo(33));
            Assert.That((int)await _argsParser.Parse<SignedTokens>(ImmutableList.Create("T44")), Is.EqualTo(44));
        }

        [Test]
        public async Task accepts_optional_positive_sign()
        {
            Assert.That((int)await _argsParser.Parse<Pokeyen>(ImmutableList.Create("P+11")), Is.EqualTo(11));
            Assert.That((int)await _argsParser.Parse<Tokens>(ImmutableList.Create("T+22")), Is.EqualTo(22));
            Assert.That((int)await _argsParser.Parse<SignedPokeyen>(ImmutableList.Create("P+33")), Is.EqualTo(33));
            Assert.That((int)await _argsParser.Parse<SignedTokens>(ImmutableList.Create("T+44")), Is.EqualTo(44));
        }

        [Test]
        public void rejects_wrong_prefix()
        {
            ArgsParseFailure exPokeyen = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                .Parse<Pokeyen>(ImmutableList.Create("X11")))!;
            Assert.That(exPokeyen.Message, Is.EqualTo("did not recognize 'X11' as a 'P'-prefixed number"));
            ArgsParseFailure exTokens = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                .Parse<Tokens>(ImmutableList.Create("X22")))!;
            Assert.That(exTokens.Message, Is.EqualTo("did not recognize 'X22' as a 'T'-prefixed number"));
            ArgsParseFailure exSignedPokeyen = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                .Parse<SignedPokeyen>(ImmutableList.Create("X33")))!;
            Assert.That(exSignedPokeyen.Message, Is.EqualTo("did not recognize 'X33' as a 'P'-prefixed number"));
            ArgsParseFailure exSignedTokens = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                .Parse<SignedTokens>(ImmutableList.Create("X44")))!;
            Assert.That(exSignedTokens.Message, Is.EqualTo("did not recognize 'X44' as a 'T'-prefixed number"));
        }

        [Test]
        public void rejects_negative_if_not_signed()
        {
            ArgsParseFailure exPokeyen = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                .Parse<Pokeyen>(args: ImmutableList.Create("P-11")))!;
            Assert.That(exPokeyen.Message, Is.EqualTo("'P-11' cannot be less than P0"));
            ArgsParseFailure exTokens = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                .Parse<Tokens>(args: ImmutableList.Create("T-22")))!;
            Assert.That(exTokens.Message, Is.EqualTo("'T-22' cannot be less than T0"));
        }

        [Test]
        public async Task accepts_negative_if_signed()
        {
            int resultPokeyen = await _argsParser.Parse<SignedPokeyen>(args: ImmutableList.Create("P-11"));
            int resultTokens = await _argsParser.Parse<SignedTokens>(args: ImmutableList.Create("T-22"));
            Assert.That(resultPokeyen, Is.EqualTo(-11));
            Assert.That(resultTokens, Is.EqualTo(-22));
        }
    }

    [Test]
    public async Task TestTimeSpanParser()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new TimeSpanParser());

        var result1 = await argsParser.Parse<TimeSpan>(args: ImmutableList.Create("8w3d20h48m5s"));
        var result2 = await argsParser.Parse<TimeSpan>(args: ImmutableList.Create("90d"));

        var expected = new TimeSpan(
            days: 8 * 7 + 3,
            hours: 20,
            minutes: 48,
            seconds: 5);
        Assert.That(result1, Is.EqualTo(expected));
        Assert.That(result2, Is.EqualTo(TimeSpan.FromDays(90)));

        ArgsParseFailure ex1 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<TimeSpan>(args: ImmutableList.Create("5s3d")))!;
        ArgsParseFailure ex2 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<TimeSpan>(args: ImmutableList.Create("asdasdasd")))!;
        Assert.That(ex1.Message, Does.Contain("did not recognize '5s3d' as a duration"));
        Assert.That(ex2.Message, Does.Contain("did not recognize 'asdasdasd' as a duration"));
    }

    [Test]
    public async Task TestUserParser()
    {
        const string username = "some_name";
        const string displayName = "名前";
        var origUser = new User(
            id: "1234567890", name: username, twitchDisplayName: displayName, simpleName: username,
            color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0);
        var userRepoMock = new Mock<IUserRepo>();
        userRepoMock
            .Setup(r => r.FindBySimpleName(username))
            .ReturnsAsync(origUser);
        userRepoMock
            .Setup(r => r.FindByDisplayName(displayName))
            .ReturnsAsync(origUser);
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new UserParser(userRepoMock.Object));

        var resultUser = await argsParser.Parse<User>(args: ImmutableList.Create(username));
        Assert.That(resultUser, Is.EqualTo(origUser));
        var resultUserPrefixed = await argsParser.Parse<User>(args: ImmutableList.Create('@' + username));
        Assert.That(resultUserPrefixed, Is.EqualTo(origUser));
        var resultUserDisplayName = await argsParser.Parse<User>(args: ImmutableList.Create(displayName));
        Assert.That(resultUserDisplayName, Is.EqualTo(origUser));

        ArgsParseFailure ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<User>(args: ImmutableList.Create("some_unknown_name")))!;
        Assert.That(ex.Message, Is.EqualTo("did not recognize a user with the name 'some_unknown_name'"));
        ArgsParseFailure exUserPrefixed = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<User>(args: ImmutableList.Create("@some_unknown_name")))!;
        Assert.That(exUserPrefixed.Message,
            Is.EqualTo("did not recognize a user with the name 'some_unknown_name'"));
        ArgsParseFailure exDisplayName = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<User>(args: ImmutableList.Create("なまえ")))!;
        Assert.That(exDisplayName.Message, Is.EqualTo("did not recognize a user with the name 'なまえ'"));
    }

    [Test]
    public async Task TestManyOfParser()
    {
        var argsParser = new ArgsParser();
        argsParser.AddArgumentParser(new PositiveIntParser());
        argsParser.AddArgumentParser(new ManyOfParser(argsParser));

        ImmutableList<PositiveInt> threeInts = await argsParser
            .Parse<ManyOf<PositiveInt>>(ImmutableList.Create("1", "3", "2"));
        CollectionAssert.AreEqual(new[] { 1, 3, 2 }, threeInts.Select(i => i.Number));

        ImmutableList<PositiveInt> zeroInts = await argsParser
            .Parse<ManyOf<PositiveInt>>(ImmutableList.Create<string>());
        CollectionAssert.AreEqual(Array.Empty<int>(), zeroInts.Select(i => i.Number));

        ArgsParseFailure failure = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
            .Parse<ManyOf<PositiveInt>>(ImmutableList.Create<string>("1", "c", "2")))!;
        Assert.That(failure.Message, Is.EqualTo("did not recognize 'c' as a number"));
    }
}
