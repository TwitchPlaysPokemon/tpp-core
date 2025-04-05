using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using TPP.ArgsParsing;
using TPP.ArgsParsing.TypeParsers;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Match;
using TPP.Model;

namespace TPP.Core.Tests.Commands.Definitions;

public class BettingCommandsTest
{
    private static User MockUser(string name) => new User(
        id: Guid.NewGuid().ToString(),
        name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
        firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
        lastMessageAt: null, pokeyen: 0, tokens: 0,
        selectedBadge: null);

    private static Message MockMessage(User user, MessageSource? source = null) =>
        new(user, string.Empty, source ?? new MessageSource.PrimaryChat(), string.Empty);

    private static readonly ArgsParser ArgsParser;

    static BettingCommandsTest()
    {
        ArgsParser = new ArgsParser();
        ArgsParser.AddArgumentParser(new PositiveIntParser());
        ArgsParser.AddArgumentParser(new PokeyenParser());
        ArgsParser.AddArgumentParser(new PercentageParser());
        ArgsParser.AddArgumentParser(new SideParser());
        ArgsParser.AddArgumentParser(new OneOfParser(ArgsParser));
    }

    [TestFixture]
    public class Bet
    {
        [Test]
        public async Task deny_if_betting_not_available()
        {
            var commands = new BettingCommands(() => null);

            CommandResult result = await commands.Bet(new CommandContext(MockMessage(MockUser("me")),
                ImmutableList.Create("!bet 1 blue"), ArgsParser));

            Assert.That(result.Response, Is.EqualTo("betting not available right now"));
        }

        [Test]
        public async Task deny_if_betting_closed()
        {
            var bettingPeriodMock = Substitute.For<IBettingPeriod<User>>();
            bettingPeriodMock.IsBettingOpen.Returns(false);
            var commands = new BettingCommands(() => bettingPeriodMock);

            CommandResult result = await commands.Bet(new CommandContext(MockMessage(MockUser("me")),
                ImmutableList.Create("!bet 1 blue"), ArgsParser));

            Assert.That(result.Response, Is.EqualTo("betting is already closed"));
        }

        [Test]
        public async Task only_allow_from_chat()
        {
            User user = MockUser("me");
            var bettingPeriodMock = Substitute.For<IBettingPeriod<User>>();
            var commands = new BettingCommands(() => bettingPeriodMock);

            CommandResult result = await commands.Bet(new CommandContext(
                MockMessage(user, source: new MessageSource.Whisper()),
                ImmutableList.Create("100", "blue"), ArgsParser));

            Assert.That(result.Response, Is.EqualTo("you may only bet through chat"));
        }

        [Test]
        public async Task propagate_failure()
        {
            User user = MockUser("me");
            var bettingShopMock = Substitute.For<IBettingShop<User>>();
            var bettingPeriodMock = Substitute.For<IBettingPeriod<User>>();
            bettingPeriodMock.IsBettingOpen.Returns(true);
            bettingPeriodMock.BettingShop.Returns(bettingShopMock);
            bettingShopMock.PlaceBet(user, Side.Blue, 100)
                .Returns(Task.FromResult((PlaceBetFailure?)new PlaceBetFailure.InsufficientFunds(50)));
            var commands = new BettingCommands(() => bettingPeriodMock);

            CommandResult result = await commands.Bet(new CommandContext(MockMessage(user),
                ImmutableList.Create("100", "blue"), ArgsParser));

            Assert.That(result.Response, Is.EqualTo("insufficient funds, you only have 50 pokeyen available"));
        }

        [Test]
        public async Task announce_in_chat()
        {
            User user = MockUser("me");
            var bettingPeriodMock = Substitute.For<IBettingPeriod<User>>();
            var bettingShopMock = Substitute.For<IBettingShop<User>>();
            bettingPeriodMock.IsBettingOpen.Returns(true);
            bettingPeriodMock.BettingShop.Returns(bettingShopMock);
            bettingShopMock.PlaceBet(user, Side.Blue, 100)
                .Returns(Task.FromResult((PlaceBetFailure?)null));
            var commands = new BettingCommands(() => bettingPeriodMock);

            CommandResult result = await commands.Bet(new CommandContext(MockMessage(user),
                ImmutableList.Create("100", "blue"), ArgsParser));

            Assert.That(result.Response, Is.EqualTo("placed a P100 bet on Blue."));
        }
    }
}
