using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using TPP.ArgsParsing;
using TPP.ArgsParsing.TypeParsers;
using TPP.Common;
using TPP.Core.Chat;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests.Commands.Definitions
{
    public class BadgeCommandsTest
    {
        private static User MockUser(string name, PkmnSpecies? selectedBadge = null) => new User(
            id: Guid.NewGuid().ToString(),
            name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0,
            selectedBadge: selectedBadge);

        private static Message MockMessage(User user, string text = "") =>
            new(user, text, MessageSource.Chat, string.Empty);

        private IBadgeRepo _badgeRepoMock = null!;
        private IBadgeStatsRepo _badgeStatsRepoMock = null!;
        private IUserRepo _userRepoMock = null!;
        private IMessageSender _messageSender = null!;
        private ArgsParser _argsParser = null!;

        private BadgeCommands _badgeCommands = null!;

        [SetUp]
        public void SetUp()
        {
            _badgeRepoMock = Substitute.For<IBadgeRepo>();
            _badgeStatsRepoMock = Substitute.For<IBadgeStatsRepo>();
            _userRepoMock = Substitute.For<IUserRepo>();
            _messageSender = Substitute.For<IMessageSender>();
            _badgeCommands = new BadgeCommands(
                _badgeRepoMock, _badgeStatsRepoMock, _userRepoMock, _messageSender,
                ImmutableHashSet<PkmnSpecies>.Empty);
            _argsParser = new ArgsParser();
            _argsParser.AddArgumentParser(new OptionalParser(_argsParser));
            _argsParser.AddArgumentParser(new UserParser(_userRepoMock));
            _argsParser.AddArgumentParser(new AnyOrderParser(_argsParser));
            _argsParser.AddArgumentParser(new PositiveIntParser());
        }

        [TestFixture]
        private class Badges : BadgeCommandsTest
        {
            [Test]
            public async Task TestBadgesSelf()
            {
                User user = MockUser("MockUser");
                var species1 = PkmnSpecies.RegisterName("1", "Einsmon");
                var species2 = PkmnSpecies.RegisterName("22", "Zwozwomon");
                var species3 = PkmnSpecies.RegisterName("13", "Drölfmon");
                _userRepoMock
                    .FindBySimpleName(user.SimpleName)
                    .Returns(user);
                _badgeRepoMock
                    .CountByUserPerSpecies(user.Id)
                    .Returns(new Dictionary<PkmnSpecies, int>
                    {
                        [species1] = 3,
                        [species2] = 6,
                        [species3] = 9,
                    }.ToImmutableSortedDictionary());

                CommandResult result = await _badgeCommands.Badges(new CommandContext(MockMessage(user),
                    ImmutableList<string>.Empty, _argsParser));

                const string response = "Your badges: 3x #001 Einsmon, 9x #013 Drölfmon, 6x #022 Zwozwomon";
                Assert.That(result.Response, Is.EqualTo(response));
            }

            [Test]
            public async Task TestBadgesOther()
            {
                User user = MockUser("MockUser");
                var species1 = PkmnSpecies.RegisterName("1", "Einsmon");
                var species2 = PkmnSpecies.RegisterName("22", "Zwozwomon");
                var species3 = PkmnSpecies.RegisterName("13", "Drölfmon");
                _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species1, species2, species3 }));
                User otherUser = MockUser("Someone_Else");
                _userRepoMock
                    .FindBySimpleName(otherUser.SimpleName)
                    .Returns(otherUser);
                _badgeRepoMock
                    .CountByUserPerSpecies(otherUser.Id)
                    .Returns(new Dictionary<PkmnSpecies, int>
                    {
                        [species1] = 12,
                        [species2] = 23,
                        [species3] = 34,
                    }.ToImmutableSortedDictionary());

                CommandResult result = await _badgeCommands.Badges(new CommandContext(MockMessage(user),
                    ImmutableList.Create("sOmeOnE_eLsE"), _argsParser));

                const string response =
                    "Someone_Else's badges: 12x #001 Einsmon, 34x #013 Drölfmon, 23x #022 Zwozwomon";
                Assert.That(result.Response, Is.EqualTo(response));
            }

            [Test]
            public void TestBadgesUserNotFound()
            {
                _argsParser.AddArgumentParser(new PkmnSpeciesParser(Array.Empty<PkmnSpecies>()));
                User user = MockUser("MockUser");
                ArgsParseFailure exception = Assert.ThrowsAsync<ArgsParseFailure>(() => _badgeCommands.Badges(
                    new CommandContext(MockMessage(user), ImmutableList.Create("@someone_unknown"), _argsParser)))!;
                Assert.That(exception.Message, Is.EqualTo("did not recognize a user with the name 'someone_unknown'"));
            }

            [Test]
            public async Task TestSpeciesOverNameIfAmbiguous()
            {
                User user = MockUser("MockUser");
                PkmnSpecies species = PkmnSpecies.RegisterName("1", "PersonMon");
                _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
                User otherUser = MockUser("PersonMon");
                _userRepoMock
                    .FindBySimpleName(otherUser.SimpleName)
                    .Returns(otherUser);
                _badgeRepoMock
                    .CountByUserAndSpecies(user.Id, species)
                    .Returns(1);
                _badgeRepoMock
                    .CountByUserPerSpecies(otherUser.Id)
                    .Returns(ImmutableSortedDictionary<PkmnSpecies, int>.Empty);

                CommandResult resultAmbiguous = await _badgeCommands.Badges(new CommandContext(MockMessage(user),
                    ImmutableList.Create("PersonMon"), _argsParser));
                Assert.That(resultAmbiguous.Response, Is.EqualTo("You have 1x #001 PersonMon badges."));

                CommandResult resultDisambiguated = await _badgeCommands.Badges(new CommandContext(MockMessage(user),
                    ImmutableList.Create("@PersonMon"), _argsParser));
                Assert.That(resultDisambiguated.Response, Is.EqualTo("PersonMon has no badges."));
            }

            [Test]
            public async Task TestSpeciesAndUserInAnyOrder()
            {
                User user = MockUser("User");
                PkmnSpecies species = PkmnSpecies.RegisterName("1", "Species");
                _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
                _userRepoMock
                    .FindBySimpleName(user.SimpleName)
                    .Returns(user);
                _badgeRepoMock
                    .CountByUserAndSpecies(user.Id, species)
                    .Returns(1);
                _badgeRepoMock
                    .CountByUserPerSpecies(user.Id)
                    .Returns(ImmutableSortedDictionary<PkmnSpecies, int>.Empty);

                CommandResult result1 = await _badgeCommands.Badges(new CommandContext(MockMessage(user),
                    ImmutableList.Create("Species", "User"), _argsParser));
                CommandResult result2 = await _badgeCommands.Badges(new CommandContext(MockMessage(user),
                    ImmutableList.Create("User", "Species"), _argsParser));

                Assert.That(result2.Response, Is.EqualTo(result1.Response));
                Assert.That(result1.Response, Is.EqualTo("User has 1x #001 Species badges."));
            }
        }

        [Test]
        public async Task TestUnselectBadge()
        {
            PkmnSpecies species = PkmnSpecies.RegisterName("1", "Mon");
            User user = MockUser("MockUser", selectedBadge: species);

            CommandResult result = await _badgeCommands.UnselectBadge(new CommandContext(MockMessage(user),
                ImmutableList<string>.Empty, _argsParser));

            Assert.That(result.Response, Is.EqualTo("#001 Mon badge unequipped."));
            await _userRepoMock.Received(1).SetSelectedBadge(user, null);
        }

        [Test]
        public async Task TestUnselectBadgeNoneEquipped()
        {
            User user = MockUser("MockUser", selectedBadge: null);

            CommandResult result = await _badgeCommands.UnselectBadge(new CommandContext(MockMessage(user),
                ImmutableList<string>.Empty, _argsParser));

            Assert.That(result.Response, Is.EqualTo("You don't have a badge equipped."));
            Assert.That(_userRepoMock.ReceivedCalls(), Is.Empty);
        }

        [Test]
        public async Task TestSelectBadge()
        {
            User user = MockUser("MockUser");
            PkmnSpecies species = PkmnSpecies.RegisterName("1", "Mon");
            _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
            _badgeRepoMock.HasUserBadge(user.Id, species).Returns(true);

            CommandResult result = await _badgeCommands.SelectBadge(new CommandContext(MockMessage(user),
                ImmutableList.Create("#1"), _argsParser));

            await _userRepoMock.Received(1).SetSelectedBadge(user, species);
            Assert.That(result.Response, Is.EqualTo("#001 Mon selected as badge."));
        }

        [Test]
        public async Task TestSelectBadgeNotOwner()
        {
            User user = MockUser("MockUser");
            PkmnSpecies species = PkmnSpecies.RegisterName("1", "Mon");
            _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
            _badgeRepoMock.HasUserBadge(user.Id, species).Returns(false);

            CommandResult result = await _badgeCommands.SelectBadge(new CommandContext(MockMessage(user),
                ImmutableList.Create("#1"), _argsParser));

            Assert.That(_userRepoMock.ReceivedCalls().Count(), Is.EqualTo(0));
            Assert.That(result.Response, Is.EqualTo("#001 Mon is not an owned badge."));
        }

        [Test]
        public void TestSelectUnknownBadge()
        {
            _argsParser.AddArgumentParser(new PkmnSpeciesParser(Array.Empty<PkmnSpecies>()));
            User user = MockUser("MockUser");
            ArgsParseFailure failure = Assert.ThrowsAsync<ArgsParseFailure>(() =>
                _badgeCommands.SelectBadge(new CommandContext(MockMessage(user),
                    ImmutableList.Create("#123"), _argsParser)))!;
            Assert.That(failure.Message, Is.EqualTo("did not recognize species '#123'"));
        }

        [Test]
        public async Task TestCheckBadge()
        {
            User user = MockUser("MockUser");
            PkmnSpecies species = PkmnSpecies.RegisterName("1", "Mon");
            _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
            _badgeStatsRepoMock.GetBadgeStatsForSpecies(species)
                .Returns(new BadgeStat(species, 5, 5, 5, 5, 0.00001d));
            _badgeRepoMock.FindOwnerIdsForSpecies(species)
                .Returns(ImmutableList.Create("1", "2", "3"));
            _userRepoMock.FindById("1").Returns(MockUser("Owner1"));
            _userRepoMock.FindById("2").Returns(MockUser("Owner2"));
            _userRepoMock.FindById("3").Returns(MockUser("Owner3"));

            CommandResult result = await _badgeCommands.CheckBadge(new CommandContext(MockMessage(user),
                ImmutableList.Create("#1"), _argsParser));

            Assert.That(result.Response, Is.EqualTo(
                "#001 Mon badges existing: 5, " +
                "logarithmic inverse rarity: 11.5, higher is better, owned by Owner1, Owner2, Owner3"));
        }

        [Test]
        public async Task TestPokedex()
        {
            User user = MockUser("MockUser");
            _badgeRepoMock
                .CountByUserPerSpecies(user.Id)
                .Returns(new Dictionary<PkmnSpecies, int>
                {
                    [PkmnSpecies.OfId("1")] = 12,
                    [PkmnSpecies.OfId("2")] = 23,
                    [PkmnSpecies.OfId("3")] = 34,
                }.ToImmutableSortedDictionary());

            CommandResult result = await _badgeCommands.Pokedex(new CommandContext(MockMessage(user),
                ImmutableList<string>.Empty, _argsParser));

            Assert.That(result.Response, Is.EqualTo("You have collected 3 distinct Pokémon badge(s)"));
        }

        [Test]
        public async Task TestGiftBadgeSuccessful()
        {
            PkmnSpecies species = PkmnSpecies.RegisterName("1", "species");
            _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
            User user = MockUser("MockUser");
            User recipient = MockUser("Recipient");
            _userRepoMock.FindBySimpleName("recipient").Returns(Task.FromResult((User?)recipient));
            Badge badge1 = new("badge1", user.Id, species, Badge.BadgeSource.ManualCreation, Instant.MinValue);
            Badge badge2 = new("badge2", user.Id, species, Badge.BadgeSource.ManualCreation, Instant.MinValue);
            Badge badge3 = new("badge3", user.Id, species, Badge.BadgeSource.ManualCreation, Instant.MinValue);
            _badgeRepoMock.FindByUserAndSpecies(user.Id, species, 2)
                .Returns(ImmutableList.Create(badge1, badge2, badge3));

            CommandResult result = await _badgeCommands.GiftBadge(new CommandContext(MockMessage(user),
                ImmutableList.Create("recipient", "species", "2"), _argsParser));

            Assert.That(result.Response, Is.EqualTo("You successfully gifted 2 #001 species badges to Recipient!"));
            Assert.That(result.ResponseTarget, Is.EqualTo(ResponseTarget.NoneIfChat));
            IDictionary<string, object?> data = new Dictionary<string, object?> { ["gifter"] = user.Id };
            await _badgeRepoMock.Received(1).TransferBadges(
                Arg.Is<IImmutableList<Badge>>(list => list.SequenceEqual(ImmutableList.Create(badge1, badge2))),
                recipient.Id,
                "gift",
                Arg.Is<IDictionary<string, object?>>(dict => dict.DictionaryEqual(data)));
        }

        [Test]
        public async Task TestGiftBadgeNotOwned()
        {
            PkmnSpecies species = PkmnSpecies.RegisterName("1", "species");
            _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
            User user = MockUser("MockUser");
            User recipient = MockUser("Recipient");
            _userRepoMock.FindBySimpleName("recipient").Returns(Task.FromResult((User?)recipient));
            _badgeRepoMock.FindByUserAndSpecies(user.Id, species, 1)
                .Returns(ImmutableList<Badge>.Empty);

            CommandResult result = await _badgeCommands.GiftBadge(new CommandContext(MockMessage(user),
                ImmutableList.Create("recipient", "species"), _argsParser));

            Assert.That(result.Response, Is.EqualTo("You tried to gift 1 #001 species badges, but you only have 0."));
            Assert.That(result.ResponseTarget, Is.EqualTo(ResponseTarget.Source));
            await _badgeRepoMock.DidNotReceive().TransferBadges(
                    Arg.Any<IImmutableList<Badge>>(),
                    Arg.Any<string?>(),
                    Arg.Any<string>(),
                    Arg.Any<IDictionary<string, object?>>());
        }
    }
}
