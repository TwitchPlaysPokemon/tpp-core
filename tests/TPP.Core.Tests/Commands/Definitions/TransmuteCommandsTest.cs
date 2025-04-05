using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using TPP.ArgsParsing;
using TPP.ArgsParsing.TypeParsers;
using TPP.Common;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests.Commands.Definitions;

public class TransmuteCommandsTest
{
    private static User MockUser(string name, PkmnSpecies? selectedBadge = null) => new(
        id: Guid.NewGuid().ToString(),
        name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
        firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
        lastMessageAt: null, pokeyen: 0, tokens: 0,
        selectedBadge: selectedBadge);

    private static Message MockMessage(User user, string text = "") =>
        new(user, text, new MessageSource.PrimaryChat(), string.Empty);

    private IBadgeRepo _badgeRepoMock = null!;
    private ArgsParser _argsParser = null!;

    [SetUp]
    public void SetUp()
    {
        _badgeRepoMock = Substitute.For<IBadgeRepo>();
        _argsParser = new ArgsParser();
        _argsParser.AddArgumentParser(new AnyOrderParser(_argsParser));
        _argsParser.AddArgumentParser(new ManyOfParser(_argsParser));
        _argsParser.AddArgumentParser(new OptionalParser(_argsParser));
        _argsParser.AddArgumentParser(new TokensParser());
    }

    [Test]
    public async Task TestTransmute()
    {
        PkmnSpecies species = PkmnSpecies.RegisterName("1", "mon");
        _argsParser.AddArgumentParser(new PkmnSpeciesParser([species]));
        User user = MockUser("MockUser");
        _badgeRepoMock.FindByUserAndSpecies(user.Id, species, null)
            .Returns(ImmutableList<Badge>.Empty);

        var transmuter = Substitute.For<ITransmuter>();
        transmuter.Transmute(user, 1, Arg.Any<IImmutableList<PkmnSpecies>>())
            .Returns(new Badge("badge1", user.Id, species, Badge.BadgeSource.Transmutation,
                Instant.FromUnixTimeSeconds(0)));

        TransmuteCommands transmuteCommands = new(transmuter,
            transmutationCooldown: Duration.Zero, messageDelay: Duration.Zero);
        {
            CommandResult result = await transmuteCommands.Transmute(new CommandContext(MockMessage(user),
                ImmutableList.Create("T1", "mon", "mon", "mon"), _argsParser));
            Assert.That(result.Response, Is.EqualTo($"{user.Name} transmuted #001 mon, #001 mon and #001 mon, " +
                                                    "and the result is #001 mon!"));
        }
        {
            CommandResult result = await transmuteCommands.Transmute(new CommandContext(MockMessage(user),
                ImmutableList.Create("mon", "mon", "mon", "T1"), _argsParser));
            Assert.That(result.Response, Is.EqualTo($"{user.Name} transmuted #001 mon, #001 mon and #001 mon, " +
                                                    "and the result is #001 mon!"));
        }
    }
}
