using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Moq;
using NodaTime;
using NodaTime.Text;
using NUnit.Framework;
using TPP.ArgsParsing.TypeParsers;
using TPP.ArgsParsing.Types;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.ArgsParsing.Tests
{
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
            Assert.AreEqual(123, (int)int1);
            Assert.AreEqual(123, (int)int2);
            Assert.AreEqual("foo", string1);
            Assert.AreEqual("foo", string2);

            var ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<AnyOrder<SignedInt, string>>(ImmutableList.Create("foo", "bar")));
            Assert.AreEqual(2, ex.Failures.Count);
            Assert.AreEqual("did not recognize 'foo' as a number, or did not recognize 'bar' as a number", ex.Message);
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
            Assert.AreEqual(123, (int)optionalInt.Value);
            Assert.AreEqual("abc", str);
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
            Assert.AreEqual(1, (int)num1);
            Assert.AreEqual("a", str);
            Assert.AreEqual(2, (int)num2);
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
            Assert.AreEqual(1, (int)num);
            Assert.AreEqual(Instant.FromUtc(2000, 1, 1, 12, 0), instant);
        }

        [Test]
        public async Task TestInstantParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new InstantParser());

            var result1 = await argsParser.Parse<Instant>(args: ImmutableList.Create("2020-03-22", "01:59:20Z"));
            var result2 = await argsParser.Parse<Instant>(args: ImmutableList.Create("2020-03-22T01:59:20Z"));

            Instant refInstant = InstantPattern.General.Parse("2020-03-22T01:59:20Z").Value;
            Assert.AreEqual(refInstant, result1);
            Assert.AreEqual(refInstant, result2);
            Assert.AreEqual(result1, result2);

            var ex1 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<Instant>(ImmutableList.Create("2020-03-22T01:59:20+02")));
            var ex2 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<Instant>(ImmutableList.Create("asdasdasd")));
            Assert.AreEqual("did not recognize '2020-03-22T01:59:20+02' as a UTC-instant", ex1.Message);
            Assert.AreEqual("did not recognize 'asdasdasd' as a UTC-instant", ex2.Message);
        }

        [Test]
        public async Task TestHexColorParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new HexColorParser());

            Assert.AreEqual("#ABCDEF", (await argsParser
                .Parse<HexColor>(args: ImmutableList.Create("#abcdef"))).StringWithHash);
            Assert.AreEqual("#ABCDEF", (await argsParser
                .Parse<HexColor>(args: ImmutableList.Create("abcdef"))).StringWithHash);
            Assert.AreEqual("#0F9D5A", (await argsParser
                .Parse<HexColor>(args: ImmutableList.Create("#0f9D5a"))).StringWithHash);

            var ex1 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<HexColor>(ImmutableList.Create("blabla")));
            var ex2 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<HexColor>(ImmutableList.Create("#abc")));
            var ex3 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<HexColor>(ImmutableList.Create("#bcdefg")));
            Assert.AreEqual("'blabla' is not a valid hex color", ex1.Message);
            Assert.AreEqual("'#abc' must be a 6-character hex code consisting of 0-9 and A-F, " +
                            "for example '#FF0000' for pure red.", ex2.Message);
            Assert.AreEqual("'#bcdefg' must be a 6-character hex code consisting of 0-9 and A-F, " +
                            "for example '#FF0000' for pure red.", ex3.Message);
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
            Assert.AreEqual(123, (int)result1.Item1.Value);
            Assert.IsFalse(result2.Item1.IsPresent);
            Assert.IsTrue(result2.Item2.IsPresent);
            Assert.AreEqual("foo", result2.Item2.Value);

            var exUnrecognized = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<OneOf<SignedInt, Instant>>(ImmutableList.Create("foo")));
            Assert.AreEqual(2, exUnrecognized.Failures.Count);
            const string errorText = "did not recognize 'foo' as a number, or did not recognize 'foo' as a UTC-instant";
            Assert.AreEqual(errorText, exUnrecognized.Message);
            var exTooManyArgs = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<OneOf<SignedInt, SignedInt>>(ImmutableList.Create("123", "234")));
            Assert.AreEqual("too many arguments", exTooManyArgs.Message);
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
            Assert.AreEqual(123, (int)result1.Value);
            Assert.IsFalse(result2.IsPresent);
            Assert.IsFalse(result3.IsPresent);
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

            Assert.AreEqual(species, resultById);
            Assert.AreEqual(species, resultByPaddedId);
            Assert.AreEqual(species, resultByName1);
            Assert.AreEqual(species, resultByName2);

            ArgsParseFailure exNotPrefixed = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<PkmnSpecies>(args: ImmutableList.Create(speciesId)));
            Assert.AreEqual("Please prefix with '#' to supply and pokedex number", exNotPrefixed.Message);
            ArgsParseFailure exUnknown = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<PkmnSpecies>(args: ImmutableList.Create("unknown")));
            Assert.AreEqual(
                "No pokemon with the name 'unknown' was recognized. Please supply a valid name, " +
                "or prefix with '#' to supply and pokedex number instead", exUnknown.Message);
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

            Assert.AreEqual(species, resultById);
            Assert.AreEqual(species, result1);
            Assert.AreEqual(species, result2);
            Assert.AreEqual(species, result3);
            Assert.AreEqual(species, result4);
            Assert.AreEqual(species, result5);

            ArgsParseFailure exNotRecognized = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<PkmnSpecies>(args: ImmutableList.Create("mahina", "aaaaaa")));
            Assert.AreEqual(
                "No pokemon with the name 'mahina' was recognized. Please supply a valid name, " +
                "or prefix with '#' to supply and pokedex number instead", exNotRecognized.Message);
            ArgsParseFailure exNoAccidentalHashRemoval = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<PkmnSpecies>(args: ImmutableList.Create("#mahinapea")));
            Assert.AreEqual("did not recognize species '#mahinapea'", exNoAccidentalHashRemoval.Message);
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
                Assert.AreEqual(11, (int)await _argsParser.Parse<Pokeyen>(ImmutableList.Create("P11")));
                Assert.AreEqual(22, (int)await _argsParser.Parse<Tokens>(ImmutableList.Create("T22")));
                Assert.AreEqual(33, (int)await _argsParser.Parse<SignedPokeyen>(ImmutableList.Create("P33")));
                Assert.AreEqual(44, (int)await _argsParser.Parse<SignedTokens>(ImmutableList.Create("T44")));
            }

            [Test]
            public async Task accepts_optional_positive_sign()
            {
                Assert.AreEqual(11, (int)await _argsParser.Parse<Pokeyen>(ImmutableList.Create("P+11")));
                Assert.AreEqual(22, (int)await _argsParser.Parse<Tokens>(ImmutableList.Create("T+22")));
                Assert.AreEqual(33, (int)await _argsParser.Parse<SignedPokeyen>(ImmutableList.Create("P+33")));
                Assert.AreEqual(44, (int)await _argsParser.Parse<SignedTokens>(ImmutableList.Create("T+44")));
            }

            [Test]
            public void rejects_wrong_prefix()
            {
                var exPokeyen = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                    .Parse<Pokeyen>(ImmutableList.Create("X11")));
                Assert.AreEqual("did not recognize 'X11' as a 'P'-prefixed number", exPokeyen.Message);
                var exTokens = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                    .Parse<Tokens>(ImmutableList.Create("X22")));
                Assert.AreEqual("did not recognize 'X22' as a 'T'-prefixed number", exTokens.Message);
                var exSignedPokeyen = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                    .Parse<SignedPokeyen>(ImmutableList.Create("X33")));
                Assert.AreEqual("did not recognize 'X33' as a 'P'-prefixed number", exSignedPokeyen.Message);
                var exSignedTokens = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                    .Parse<SignedTokens>(ImmutableList.Create("X44")));
                Assert.AreEqual("did not recognize 'X44' as a 'T'-prefixed number", exSignedTokens.Message);
            }

            [Test]
            public void rejects_negative_if_not_signed()
            {
                var exPokeyen = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                    .Parse<Pokeyen>(args: ImmutableList.Create("P-11")));
                Assert.AreEqual("'P-11' cannot be less than P0", exPokeyen.Message);
                var exTokens = Assert.ThrowsAsync<ArgsParseFailure>(() => _argsParser
                    .Parse<Tokens>(args: ImmutableList.Create("T-22")));
                Assert.AreEqual("'T-22' cannot be less than T0", exTokens.Message);
            }

            [Test]
            public async Task accepts_negative_if_signed()
            {
                int resultPokeyen = await _argsParser.Parse<SignedPokeyen>(args: ImmutableList.Create("P-11"));
                int resultTokens = await _argsParser.Parse<SignedTokens>(args: ImmutableList.Create("T-22"));
                Assert.AreEqual(-11, resultPokeyen);
                Assert.AreEqual(-22, resultTokens);
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
            Assert.AreEqual(expected, result1);
            Assert.AreEqual(TimeSpan.FromDays(90), result2);

            var ex1 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<TimeSpan>(args: ImmutableList.Create("5s3d")));
            var ex2 = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<TimeSpan>(args: ImmutableList.Create("asdasdasd")));
            Assert.IsTrue(ex1.Message.Contains("did not recognize '5s3d' as a duration"));
            Assert.IsTrue(ex2.Message.Contains("did not recognize 'asdasdasd' as a duration"));
        }

        [Test]
        public async Task TestUserParser()
        {
            const string username = "some_name";
            var origUser = new User(
                id: "1234567890", name: username, twitchDisplayName: username.ToUpper(), simpleName: username,
                color: null,
                firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
                lastMessageAt: null, pokeyen: 0, tokens: 0);
            var userRepoMock = new Mock<IUserRepo>();
            userRepoMock
                .Setup(r => r.FindBySimpleName(username))
                .ReturnsAsync(origUser);
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new UserParser(userRepoMock.Object));

            var resultUser = await argsParser.Parse<User>(args: ImmutableList.Create(username));
            Assert.AreEqual(origUser, resultUser);
            var resultUserPrefixed = await argsParser.Parse<User>(args: ImmutableList.Create('@' + username));
            Assert.AreEqual(origUser, resultUserPrefixed);

            var ex = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<User>(args: ImmutableList.Create("some_unknown_name")));
            Assert.AreEqual("did not recognize a user with the name 'some_unknown_name'", ex.Message);
            var exUserPrefixed = Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser
                .Parse<User>(args: ImmutableList.Create("@some_unknown_name")));
            Assert.AreEqual("did not recognize a user with the name 'some_unknown_name'", exUserPrefixed.Message);
        }
    }
}
