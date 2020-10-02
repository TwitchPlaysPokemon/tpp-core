using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArgsParsing.TypeParsers;
using ArgsParsing.Types;
using Common;
using Moq;
using NFluent;
using NFluent.ApiChecks;
using NodaTime;
using NodaTime.Text;
using NUnit.Framework;
using Persistence.Models;
using Persistence.Repos;

namespace ArgsParsing.Tests
{
    public class TypeParsersTest
    {
        [Test]
        public async Task TestAnyOrderParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
            argsParser.AddArgumentParser(new StringParser());
            argsParser.AddArgumentParser(new IntParser());

            var args1 = ImmutableList.Create("123", "foo");
            var args2 = ImmutableList.Create("foo", "123");
            (int int1, string string1) = await argsParser.Parse<AnyOrder<int, string>>(args1);
            (int int2, string string2) = await argsParser.Parse<AnyOrder<int, string>>(args2);
            Assert.AreEqual(123, int1);
            Assert.AreEqual(123, int2);
            Assert.AreEqual("foo", string1);
            Assert.AreEqual("foo", string2);

            Check.ThatCode(
                    async () => await argsParser.Parse<AnyOrder<int, string>>(ImmutableList.Create("foo", "bar")))
                .Throws<ArgsParseFailure>()
                .WithMessage("did not recognize 'foo' as a number, or did not recognize 'bar' as a number")
                .And.WhichMember(ex => ex.Failures).HasSize(2);
        }

        [Test]
        public async Task TestAnyOrderOptional()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
            argsParser.AddArgumentParser(new OptionalParser(argsParser));
            argsParser.AddArgumentParser(new IntParser());
            argsParser.AddArgumentParser(new StringParser());

            // this used to cause a stack overflow in the any order parser
            (Optional<int> optionalInt, Optional<string> optionalString) = await argsParser
                .Parse<AnyOrder<Optional<int>, Optional<string>>>(ImmutableList<string>.Empty);
            Assert.False(optionalInt.IsPresent);
            Assert.False(optionalString.IsPresent);
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

            Check.ThatCode(async () => await argsParser.Parse<Instant>(ImmutableList.Create("2020-03-22T01:59:20+02")))
                .Throws<ArgsParseFailure>()
                .WithMessage("did not recognize '2020-03-22T01:59:20+02' as a UTC-instant");
            Check.ThatCode(async () => await argsParser.Parse<Instant>(ImmutableList.Create("asdasdasd")))
                .Throws<ArgsParseFailure>()
                .WithMessage("did not recognize 'asdasdasd' as a UTC-instant");
        }

        [Test]
        public async Task TestHexColorParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new HexColorParser());

            Assert.AreEqual("#ABCDEF", (string)await argsParser
                .Parse<HexColor>(args: ImmutableList.Create("#abcdef")));
            Assert.AreEqual("#0F9D5A", (string)await argsParser
                .Parse<HexColor>(args: ImmutableList.Create("#0f9D5a")));

            Check.ThatCode(async () => await argsParser.Parse<HexColor>(ImmutableList.Create("abcdef")))
                .Throws<ArgsParseFailure>()
                .WithMessage("'abcdef' is not a valid hex color");
            Check.ThatCode(async () => await argsParser.Parse<HexColor>(ImmutableList.Create("#abc")))
                .Throws<ArgsParseFailure>()
                .WithMessage("'#abc' must be a 6-character hex code consisting of 0-9 and A-F, " +
                             "for example '#FF0000' for pure red.");
            Check.ThatCode(async () => await argsParser.Parse<HexColor>(ImmutableList.Create("#bcdefg")))
                .Throws<ArgsParseFailure>()
                .WithMessage("'#bcdefg' must be a 6-character hex code consisting of 0-9 and A-F, " +
                             "for example '#FF0000' for pure red.");
        }

        [Test]
        public async Task TestOneOfParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new OneOfParser(argsParser));
            argsParser.AddArgumentParser(new StringParser());
            argsParser.AddArgumentParser(new IntParser());
            argsParser.AddArgumentParser(new InstantParser());

            OneOf<int, string> result1 = await argsParser.Parse<OneOf<int, string>>(ImmutableList.Create("123"));
            OneOf<int, string> result2 = await argsParser.Parse<OneOf<int, string>>(ImmutableList.Create("foo"));
            Assert.IsTrue(result1.Item1.IsPresent);
            Assert.IsFalse(result1.Item2.IsPresent);
            Assert.AreEqual(123, result1.Item1.Value);
            Assert.IsFalse(result2.Item1.IsPresent);
            Assert.IsTrue(result2.Item2.IsPresent);
            Assert.AreEqual("foo", result2.Item2.Value);

            Check.ThatCode(async () => await argsParser.Parse<OneOf<int, Instant>>(ImmutableList.Create("foo")))
                .Throws<ArgsParseFailure>()
                .WithMessage("did not recognize 'foo' as a number, or did not recognize 'foo' as a UTC-instant")
                .And.WhichMember(ex => ex.Failures).HasSize(2);
            Check.ThatCode(async () => await argsParser.Parse<OneOf<int, int>>(ImmutableList.Create("123", "234")))
                .Throws<ArgsParseFailure>()
                .WithMessage("too many arguments");
        }

        [Test]
        public async Task TestOptionalParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new OptionalParser(argsParser));
            argsParser.AddArgumentParser(new StringParser());
            argsParser.AddArgumentParser(new IntParser());

            var result1 = await argsParser.Parse<Optional<int>>(args: ImmutableList.Create("123"));
            var result2 = await argsParser.Parse<Optional<int>>(args: ImmutableList<string>.Empty);
            (Optional<int> result3, string _) = await argsParser
                .Parse<Optional<int>, string>(args: ImmutableList.Create("foo"));
            Assert.IsTrue(result1.IsPresent);
            Assert.AreEqual(123, result1.Value);
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
            PkmnSpecies resultByPaddedId = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("#0" + speciesId));
            PkmnSpecies resultByName1 = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create(speciesName));
            PkmnSpecies resultByName2 = await argsParser.Parse<PkmnSpecies>(args: ImmutableList.Create("uNiQuAmOn"));

            Assert.AreEqual(species, resultById);
            Assert.AreEqual(species, resultByPaddedId);
            Assert.AreEqual(species, resultByName1);
            Assert.AreEqual(species, resultByName2);

            Check.ThatCode(async () => await argsParser.Parse<PkmnSpecies>(ImmutableList.Create(speciesId)))
                .Throws<ArgsParseFailure>()
                .WithMessage("Please prefix with '#' to supply and pokedex number");
            Check.ThatCode(async () => await argsParser.Parse<PkmnSpecies>(ImmutableList.Create("unknown")))
                .Throws<ArgsParseFailure>()
                .WithMessage("No pokemon with the name 'unknown' was recognized. Please supply a valid name, " +
                             "or prefix with '#' to supply and pokedex number instead");
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

            Check.ThatCode(async () => await argsParser.Parse<PkmnSpecies>(ImmutableList.Create("mahina", "aaaaaa")))
                .Throws<ArgsParseFailure>()
                .WithMessage("No pokemon with the name 'mahina' was recognized. Please supply a valid name, " +
                             "or prefix with '#' to supply and pokedex number instead");
            Check.ThatCode(async () => await argsParser.Parse<PkmnSpecies>(ImmutableList.Create("#mahinapea")))
                .Throws<ArgsParseFailure>()
                .WithMessage("did not recognize species '#mahinapea'");
        }

        [Test]
        public async Task TestPrefixedNumberParsers()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new PokeyenParser());
            argsParser.AddArgumentParser(new TokensParser());

            int result1 = await argsParser.Parse<Pokeyen>(args: ImmutableList.Create("P11"));
            int result2 = await argsParser.Parse<Tokens>(args: ImmutableList.Create("T22"));
            Assert.AreEqual(11, result1);
            Assert.AreEqual(22, result2);

            Check.ThatCode(async () => await argsParser.Parse<Pokeyen>(ImmutableList.Create("X33")))
                .Throws<ArgsParseFailure>()
                .WithMessage("did not recognize 'X33' as a 'P'-prefixed number");
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

            Check.ThatCode(async () => await argsParser.Parse<TimeSpan>(ImmutableList.Create("5s3d")))
                .Throws<ArgsParseFailure>()
                .AndWhichMessage().Contains("did not recognize '5s3d' as a duration");
            Check.ThatCode(async () => await argsParser.Parse<TimeSpan>(ImmutableList.Create("asdasdasd")))
                .Throws<ArgsParseFailure>()
                .AndWhichMessage().Contains("did not recognize 'asdasdasd' as a duration");
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

            Check.ThatCode(async () => await argsParser.Parse<User>(ImmutableList.Create("some_unknown_name")))
                .Throws<ArgsParseFailure>()
                .WithMessage("did not recognize a user with the name 'some_unknown_name'");
            Check.ThatCode(async () => await argsParser.Parse<User>(ImmutableList.Create("@some_unknown_name")))
                .Throws<ArgsParseFailure>()
                .WithMessage("did not recognize a user with the name 'some_unknown_name'");
        }
    }
}
