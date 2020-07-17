using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ArgsParsing;
using ArgsParsing.TypeParsers;
using Common;
using Core.Commands;
using Core.Commands.Definitions;
using Moq;
using NUnit.Framework;
using Persistence.Models;
using Persistence.Repos;
using static Core.Tests.TestUtils;

namespace Core.Tests.Commands.Definitions
{
    public class BadgeCommandsTest
    {
        private readonly ImmutableList<string> _noArgs = ImmutableList<string>.Empty;
        private readonly User _mockUser = MockUser("MockUser");

        private Message MockMessage(string text = "") => new Message(_mockUser, text, MessageSource.Chat);

        private Mock<IBadgeRepo> _badgeRepoMock = null!;
        private Mock<IUserRepo> _userRepoMock = null!;
        private ArgsParser _argsParser = null!;

        private BadgeCommands _badgeCommands = null!;

        [SetUp]
        public void SetUp()
        {
            _badgeRepoMock = new Mock<IBadgeRepo>();
            _userRepoMock = new Mock<IUserRepo>();
            _badgeCommands = new BadgeCommands(_badgeRepoMock.Object, _userRepoMock.Object);
            _argsParser = new ArgsParser();
            _argsParser.AddArgumentParser(new OptionalParser(_argsParser));
            _argsParser.AddArgumentParser(new UserParser(_userRepoMock.Object));
            _argsParser.AddArgumentParser(new PkmnSpeciesParser());
        }

        [Test]
        public async Task TestBadgesSelf()
        {
            PkmnSpecies.RegisterName("1", "Einsmon");
            PkmnSpecies.RegisterName("22", "Zwozwomon");
            PkmnSpecies.RegisterName("13", "Drölfmon");
            _userRepoMock
                .Setup(repo => repo.FindBySimpleName(_mockUser.SimpleName))
                .ReturnsAsync(_mockUser);
            _badgeRepoMock
                .Setup(repo => repo.CountByUserPerSpecies(_mockUser.Id))
                .ReturnsAsync(new Dictionary<PkmnSpecies, int>
                {
                    [PkmnSpecies.OfId("1")] = 3,
                    [PkmnSpecies.OfId("22")] = 6,
                    [PkmnSpecies.OfId("13")] = 9,
                }.ToImmutableSortedDictionary());

            CommandResult result = await _badgeCommands.Badges(new CommandContext(MockMessage(), _noArgs, _argsParser));

            const string response = "Your badges: 3x #001 Einsmon, 9x #013 Drölfmon, 6x #022 Zwozwomon";
            Assert.AreEqual(response, result.Response);
        }

        [Test]
        public async Task TestBadgesOther()
        {
            PkmnSpecies.RegisterName("1", "Einsmon");
            PkmnSpecies.RegisterName("22", "Zwozwomon");
            PkmnSpecies.RegisterName("13", "Drölfmon");
            User otherUser = MockUser("Someone_Else");
            _userRepoMock
                .Setup(repo => repo.FindBySimpleName(otherUser.SimpleName))
                .ReturnsAsync(otherUser);
            _badgeRepoMock
                .Setup(repo => repo.CountByUserPerSpecies(otherUser.Id))
                .ReturnsAsync(new Dictionary<PkmnSpecies, int>
                {
                    [PkmnSpecies.OfId("1")] = 12,
                    [PkmnSpecies.OfId("22")] = 23,
                    [PkmnSpecies.OfId("13")] = 34,
                }.ToImmutableSortedDictionary());

            CommandResult result = await _badgeCommands.Badges(new CommandContext(MockMessage(),
                ImmutableList.Create("sOmeOnE_eLsE"), _argsParser));

            const string response = "Someone_Else's badges: 12x #001 Einsmon, 34x #013 Drölfmon, 23x #022 Zwozwomon";
            Assert.AreEqual(response, result.Response);
        }

        [Test]
        public void TestBadgesUserNotFound()
        {
            ArgsParseFailure exception = Assert.ThrowsAsync<ArgsParseFailure>(() => _badgeCommands.Badges(
                new CommandContext(MockMessage(), ImmutableList.Create("@someone_unknown"), _argsParser)));
            Assert.AreEqual("did not recognize a user with the name 'someone_unknown'", exception.Message);
        }

        [Test]
        public async Task TestSelectBadge()
        {
            PkmnSpecies.RegisterName("1", "Mon");
            PkmnSpecies species = PkmnSpecies.OfId("1");
            _badgeRepoMock.Setup(repo => repo.HasUserBadge(_mockUser.Id, species)).ReturnsAsync(true);

            CommandResult result = await _badgeCommands.SelectBadge(new CommandContext(MockMessage(),
                ImmutableList.Create("#1"), _argsParser));

            _userRepoMock.Verify(repo => repo.SetSelectedBadge(_mockUser, species), Times.Once());
            Assert.AreEqual("#001 Mon selected as badge.", result.Response);
        }

        [Test]
        public async Task TestSelectBadgeNotOwner()
        {
            PkmnSpecies.RegisterName("1", "Mon");
            PkmnSpecies species = PkmnSpecies.OfId("1");
            _badgeRepoMock.Setup(repo => repo.HasUserBadge(_mockUser.Id, species)).ReturnsAsync(false);

            CommandResult result = await _badgeCommands.SelectBadge(new CommandContext(MockMessage(),
                ImmutableList.Create("#1"), _argsParser));

            _userRepoMock.VerifyNoOtherCalls();
            Assert.AreEqual("#001 Mon is not an owned badge.", result.Response);
        }

        [Test]
        public void TestSelectUnknownBadge()
        {
            ArgsParseFailure failure = Assert.ThrowsAsync<ArgsParseFailure>(() =>
                _badgeCommands.SelectBadge(new CommandContext(MockMessage(),
                    ImmutableList.Create("#123"), _argsParser)));
            Assert.AreEqual("did not recognize species '#123'", failure.Message);
        }

        [Test]
        public async Task TestSpeciesOverNameIfAmbiguous()
        {
            PkmnSpecies.RegisterName("1", "PersonMon");
            PkmnSpecies species = PkmnSpecies.OfId("1");
            User otherUser = MockUser("PersonMon");
            _userRepoMock
                .Setup(repo => repo.FindBySimpleName(otherUser.SimpleName))
                .ReturnsAsync(otherUser);
            _badgeRepoMock
                .Setup(repo => repo.CountByUserAndSpecies(_mockUser.Id, species))
                .ReturnsAsync(1);
            _badgeRepoMock
                .Setup(repo => repo.CountByUserPerSpecies(otherUser.Id))
                .ReturnsAsync(ImmutableSortedDictionary<PkmnSpecies, int>.Empty);
            // refresh PkmnSpeciesParser's internal lookup
            _argsParser.RemoveArgumentParser<PkmnSpecies>();
            _argsParser.AddArgumentParser(new PkmnSpeciesParser());

            CommandResult resultAmbiguous = await _badgeCommands.Badges(new CommandContext(MockMessage(),
                ImmutableList.Create("PersonMon"), _argsParser));
            Assert.AreEqual("You have 1x #001 PersonMon badges.", resultAmbiguous.Response);

            CommandResult resultDisambiguated = await _badgeCommands.Badges(new CommandContext(MockMessage(),
                ImmutableList.Create("@PersonMon"), _argsParser));
            Assert.AreEqual("PersonMon has no badges.", resultDisambiguated.Response);
        }
    }
}
