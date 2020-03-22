using System;
using System.Threading.Tasks;
using Core.ArgsParsing;
using Core.ArgsParsing.TypeParsers;
using Core.ArgsParsing.Types;
using Models;
using Moq;
using NUnit.Framework;
using Persistence.Repos;

namespace Core.Tests.ArgsParsing
{
    public class TypeParsersTest
    {
        [Test]
        public async Task TestUserParser()
        {
            const string username = "some_name";
            var origUser = new User(
                id: "1234567890", name: username, twitchDisplayName: username.ToUpper(), simpleName: username,
                color: null, firstActiveAt: DateTime.UnixEpoch, lastActiveAt: DateTime.UnixEpoch,
                lastMessageAt: null, pokeyen: 0, tokens: 0);
            var userRepoMock = new Mock<IUserRepo>();
            userRepoMock
                .Setup(r => r.FindBySimpleName(username))
                .ReturnsAsync(origUser);
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new UserParser(userRepoMock.Object));

            var resultUser = await argsParser.Parse<User>(args: new[] {username});
            Assert.AreEqual(origUser, resultUser);

            Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser.Parse<User>(args: new[] {"some_unknown_name"}));
        }

        [Test]
        public async Task TestAnyOrderParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
            argsParser.AddArgumentParser(new StringParser());
            argsParser.AddArgumentParser(new IntParser());

            var args1 = new[] {"123", "foo"};
            var args2 = new[] {"foo", "123"};
            (int int1, string string1) = (await argsParser.Parse<AnyOrder<int, string>>(args1)).AsTuple();
            (int int2, string string2) = (await argsParser.Parse<AnyOrder<int, string>>(args2)).AsTuple();
            Assert.AreEqual(123, int1);
            Assert.AreEqual(123, int2);
            Assert.AreEqual("foo", string1);
            Assert.AreEqual("foo", string2);

            Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser.Parse<AnyOrder<int, string>>(new[] {"foo", "bar"}));
        }

        [Test]
        public async Task TestOptionalParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new OptionalParser(argsParser));
            argsParser.AddArgumentParser(new StringParser());
            argsParser.AddArgumentParser(new IntParser());

            var result1 = await argsParser.Parse<Optional<int>>(args: new[] {"123"});
            var result2 = await argsParser.Parse<Optional<int>>(args: new string[] { });
            (var result3, string _) = await argsParser.Parse<Optional<int>, string>(args: new[] {"foo"});
            Assert.IsTrue(result1.IsPresent);
            Assert.AreEqual(123, result1.Value);
            Assert.IsFalse(result2.IsPresent);
            Assert.IsFalse(result3.IsPresent);
        }

        [Test]
        public async Task TestPrefixedNumberParsers()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new PokeyenParser());
            argsParser.AddArgumentParser(new TokensParser());

            var result1 = await argsParser.Parse<Pokeyen>(args: new[] {"P11"});
            var result2 = await argsParser.Parse<Tokens>(args: new[] {"T22"});
            Assert.AreEqual(11, result1.Number);
            Assert.AreEqual(22, result2.Number);

            Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser.Parse<Pokeyen>(args: new[] {"X33"}));
        }

        [Test]
        public async Task TestTimeSpanParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new TimeSpanParser());

            var result1 = await argsParser.Parse<TimeSpan>(args: new[] {"8w3d20h48m5s"});
            var result2 = await argsParser.Parse<TimeSpan>(args: new[] {"90d"});

            var expected = new TimeSpan(
                days: 8 * 7 + 3,
                hours: 20,
                minutes: 48,
                seconds: 5);
            Assert.AreEqual(expected, result1);
            Assert.AreEqual(TimeSpan.FromDays(90), result2);

            Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser.Parse<TimeSpan>(args: new[] {"5s3d"}));
            Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser.Parse<TimeSpan>(args: new[] {"asdasdasd"}));
        }

        [Test]
        public async Task TestDateTimeParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new DateTimeParser());

            var result1 = await argsParser.Parse<DateTime>(args: new[] {"2020-03-22", "01:59:20Z"});
            var result2 = await argsParser.Parse<DateTime>(args: new[] {"2020-03-22T01:59:20Z"});

            var refDateTime = DateTime.SpecifyKind(DateTime.Parse("2020-03-22 01:59:20+00"), DateTimeKind.Utc);
            Assert.AreEqual(refDateTime, result1);
            Assert.AreEqual(refDateTime, result2);
            Assert.AreEqual(result1, result2);

            Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser.Parse<DateTime>(new[] {"2020-03-22T01:59:20+02"}));
            Assert.ThrowsAsync<ArgsParseFailure>(() => argsParser.Parse<DateTime>(new[] {"asdasdasd"}));
        }
    }
}
