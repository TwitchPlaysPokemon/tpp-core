using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.ArgsParsing;
using TPP.ArgsParsing.TypeParsers;
using TPP.Common;
using TPP.Core.Chat;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests.Commands.Definitions;

public class OperatorCommandsTest
{
    private static User MockUser(string name, PkmnSpecies? selectedBadge = null) => new User(
        id: Guid.NewGuid().ToString(),
        name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
        firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
        lastMessageAt: null, pokeyen: 0, tokens: 0,
        selectedBadge: selectedBadge);

    private static User MockOperator(string name, PkmnSpecies? selectedBadge = null) => new User(
        id: Guid.NewGuid().ToString(),
        name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
        firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
        lastMessageAt: null, pokeyen: 0, tokens: 0,
        selectedBadge: selectedBadge, roles: new HashSet<Role> { Role.Operator });

    private static Message MockMessage(User user, string text = "") =>
        new(user, text, MessageSource.Chat, string.Empty);

    private Mock<IUserRepo> _userRepoMock = null!;
    private ArgsParser _argsParser = null!;
    private Mock<IBank<User>> _pokeyenBankMock = null!;
    private Mock<IBank<User>> _tokensBankMock = null!;
    private Mock<IMessageSender> _messageSenderMock = null!;
    private Mock<IBadgeRepo> _badgeRepoMock = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepoMock = new Mock<IUserRepo>();
        _argsParser = new ArgsParser();
        _argsParser.AddArgumentParser(new StringParser());
        _argsParser.AddArgumentParser(new SignedPokeyenParser());
        _argsParser.AddArgumentParser(new SignedTokensParser());
        _argsParser.AddArgumentParser(new AnyOrderParser(_argsParser));
        _argsParser.AddArgumentParser(new OptionalParser(_argsParser));
        _argsParser.AddArgumentParser(new ManyOfParser(_argsParser));
        _argsParser.AddArgumentParser(new RoleParser());
        _argsParser.AddArgumentParser(new UserParser(_userRepoMock.Object));
        _argsParser.AddArgumentParser(new PositiveIntParser());
        _pokeyenBankMock = new Mock<IBank<User>>();
        _tokensBankMock = new Mock<IBank<User>>();
        _messageSenderMock = new Mock<IMessageSender>();
        _badgeRepoMock = new Mock<IBadgeRepo>();
    }

    private static bool AreTransactionsEqual(Transaction<User> tx1, Transaction<User> tx2)
        => tx1.User == tx2.User
           && tx1.Change == tx2.Change
           && tx1.Type == tx2.Type
           && tx1.AdditionalData.DictionaryEqual(tx2.AdditionalData);

    [Test]
    public async Task adjust_tokens_self()
    {
        User user = MockOperator("MockOperator");
        _userRepoMock.Setup(u => u.FindBySimpleName(user.SimpleName)).ReturnsAsync(user);
        OperatorCommands operatorCommands = new OperatorCommands(
            new StopToken(),
            _tokensBankMock.Object, _tokensBankMock.Object,
            _messageSenderMock.Object, _badgeRepoMock.Object,
            _userRepoMock.Object);

        {
            CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(user),
                ImmutableList.Create(user.Name, "T123", "because", "reason"), _argsParser));
            Assert.That(result.Response, Is.EqualTo("Your token balance was adjusted by +123. Reason: because reason"));

            var transaction = new Transaction<User>(user, 123, "manual_adjustment",
                new Dictionary<string, object?> { ["responsible_user"] = user.Id });
            _tokensBankMock.Verify(b => b.PerformTransaction(
                It.Is<Transaction<User>>(tx => AreTransactionsEqual(tx, transaction)), default), Times.Once);
        }
        {
            CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(user),
                ImmutableList.Create(user.Name, "T-123", "because"), _argsParser));
            Assert.That(result.Response, Is.EqualTo("Your token balance was adjusted by -123. Reason: because"));

            var transaction = new Transaction<User>(user, -123, "manual_adjustment",
                new Dictionary<string, object?> { ["responsible_user"] = user.Id });
            _tokensBankMock.Verify(b => b.PerformTransaction(
                It.Is<Transaction<User>>(tx => AreTransactionsEqual(tx, transaction)), default), Times.Once);
        }
        _tokensBankMock.VerifyNoOtherCalls();
        _pokeyenBankMock.VerifyNoOtherCalls();
        _messageSenderMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task adjust_tokens_someone_else()
    {
        User userSelf = MockOperator("MockUserSelf");
        User userOther = MockUser("MockUserOther");
        _userRepoMock.Setup(u => u.FindBySimpleName(userOther.SimpleName)).ReturnsAsync(userOther);
        OperatorCommands operatorCommands = new OperatorCommands(
            new StopToken(),
            _tokensBankMock.Object, _tokensBankMock.Object,
            _messageSenderMock.Object, _badgeRepoMock.Object,
            _userRepoMock.Object);

        {
            CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(userSelf),
                ImmutableList.Create(userOther.Name, "T123", "because", "reason"), _argsParser));
            Assert.That(result.Response, Is.EqualTo("MockUserOther's token balance was adjusted by +123. Reason: because reason"));

            var transaction = new Transaction<User>(userOther, 123, "manual_adjustment",
                new Dictionary<string, object?> { ["responsible_user"] = userSelf.Id });
            _tokensBankMock.Verify(b => b.PerformTransaction(
                It.Is<Transaction<User>>(tx => AreTransactionsEqual(tx, transaction)), default), Times.Once);

            const string notification = "MockUserSelf adjusted your token balance by +123. Reason: because reason";
            _messageSenderMock.Verify(m => m.SendWhisper(userOther,
                notification), Times.Once);
        }
        {
            CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(userSelf),
                ImmutableList.Create(userOther.Name, "T-123", "because"), _argsParser));
            Assert.That(result.Response, Is.EqualTo("MockUserOther's token balance was adjusted by -123. Reason: because"));

            var transaction = new Transaction<User>(userOther, -123, "manual_adjustment",
                new Dictionary<string, object?> { ["responsible_user"] = userSelf.Id });
            _tokensBankMock.Verify(b => b.PerformTransaction(
                It.Is<Transaction<User>>(tx => AreTransactionsEqual(tx, transaction)), default), Times.Once);

            const string notification = "MockUserSelf adjusted your token balance by -123. Reason: because";
            _messageSenderMock.Verify(m => m.SendWhisper(userOther,
                notification), Times.Once);
        }
        _tokensBankMock.VerifyNoOtherCalls();
        _pokeyenBankMock.VerifyNoOtherCalls();
        _messageSenderMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task TestTransferBadgeSuccessful()
    {
        PkmnSpecies species = PkmnSpecies.RegisterName("1", "species");
        _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
        User userSelf = MockOperator("MockUserSelf");
        User gifter = MockUser("Gifter");
        User recipient = MockUser("Recipient");
        OperatorCommands operatorCommands = new(
            new StopToken(),
            _tokensBankMock.Object, _tokensBankMock.Object,
            _messageSenderMock.Object, _badgeRepoMock.Object,
            _userRepoMock.Object);
        _userRepoMock.Setup(repo => repo.FindBySimpleName("gifter")).Returns(Task.FromResult((User?)gifter));
        _userRepoMock.Setup(repo => repo.FindBySimpleName("recipient")).Returns(Task.FromResult((User?)recipient));
        Badge badge1 = new("badge1", gifter.Id, species, Badge.BadgeSource.ManualCreation, Instant.MinValue);
        Badge badge2 = new("badge2", gifter.Id, species, Badge.BadgeSource.ManualCreation, Instant.MinValue);
        Badge badge3 = new("badge3", gifter.Id, species, Badge.BadgeSource.ManualCreation, Instant.MinValue);
        _badgeRepoMock.Setup(repo => repo.FindByUserAndSpecies(gifter.Id, species, 2))
            .Returns(Task.FromResult(new List<Badge> { badge1, badge2, badge3 }));

        CommandResult result = await operatorCommands.TransferBadge(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("gifter", "recipient", "species", "2", "because", "reason"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("transferred 2 #001 species badges from Gifter to Recipient. Reason: because reason"));
        Assert.That(result.ResponseTarget, Is.EqualTo(ResponseTarget.Chat));
        IDictionary<string, object?> data = new Dictionary<string, object?>
        {
            ["gifter"] = gifter.Id,
            ["responsible_user"] = userSelf.Id,
            ["reason"] = "because reason",
        };
        _badgeRepoMock.Verify(repo => repo.TransferBadges(
            It.Is<IImmutableList<Badge>>(list => list.SequenceEqual(ImmutableList.Create(badge1, badge2))),
            recipient.Id,
            "gift_remote",
            It.Is<IDictionary<string, object?>>(dict => dict.DictionaryEqual(data))));
    }

    [Test]
    public async Task TestTransferBadgeOmitReason()
    {
        PkmnSpecies species = PkmnSpecies.RegisterName("1", "species");
        _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
        User userSelf = MockOperator("MockUserSelf");
        User gifter = MockUser("Gifter");
        User recipient = MockUser("Recipient");
        OperatorCommands operatorCommands = new(
            new StopToken(),
            _tokensBankMock.Object, _tokensBankMock.Object,
            _messageSenderMock.Object, _badgeRepoMock.Object,
            _userRepoMock.Object);
        _userRepoMock.Setup(repo => repo.FindBySimpleName("gifter")).Returns(Task.FromResult((User?)gifter));
        _userRepoMock.Setup(repo => repo.FindBySimpleName("recipient")).Returns(Task.FromResult((User?)recipient));

        CommandResult result = await operatorCommands.TransferBadge(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("gifter", "recipient", "species", "2"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("Must provide a reason"));
        _badgeRepoMock.Verify(repo => repo.TransferBadges(
                It.IsAny<IImmutableList<Badge>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object?>>()),
            Times.Never);
    }

    [Test]
    public async Task TestGiftBadgeNotOwned()
    {
        PkmnSpecies species = PkmnSpecies.RegisterName("1", "species");
        _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
        User userSelf = MockOperator("MockUserSelf");
        User gifter = MockUser("Gifter");
        User recipient = MockUser("Recipient");
        OperatorCommands operatorCommands = new(
            new StopToken(),
            _tokensBankMock.Object, _tokensBankMock.Object,
            _messageSenderMock.Object, _badgeRepoMock.Object,
            _userRepoMock.Object);
        _userRepoMock.Setup(repo => repo.FindBySimpleName("gifter")).Returns(Task.FromResult((User?)gifter));
        _userRepoMock.Setup(repo => repo.FindBySimpleName("recipient")).Returns(Task.FromResult((User?)recipient));
        _badgeRepoMock.Setup(repo => repo.FindByUserAndSpecies(gifter.Id, species, 1))
            .Returns(Task.FromResult(new List<Badge>()));

        CommandResult result = await operatorCommands.TransferBadge(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("gifter", "recipient", "species", "reason"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("You tried to transfer 1 #001 species badges, but the gifter only has 0."));
        Assert.That(result.ResponseTarget, Is.EqualTo(ResponseTarget.Source));
        _badgeRepoMock.Verify(repo => repo.TransferBadges(
                It.IsAny<IImmutableList<Badge>>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object?>>()),
            Times.Never);
    }

    [Test]
    public async Task TestCreateBadge()
    {
        PkmnSpecies species = PkmnSpecies.RegisterName("1", "Species");
        _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
        User user = MockOperator("MockUser");
        User recipient = MockUser("Recipient");
        OperatorCommands operatorCommands = new(new StopToken(),
            _tokensBankMock.Object, _tokensBankMock.Object,
            _messageSenderMock.Object, _badgeRepoMock.Object,
            _userRepoMock.Object);
        _userRepoMock.Setup(repo => repo.FindBySimpleName("recipient")).Returns(Task.FromResult((User?)recipient));

        CommandResult result = await operatorCommands.CreateBadge(new CommandContext(MockMessage(user),
            ImmutableList.Create("recipient", "species", "123"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("123 badges of species #001 Species created for user Recipient."));
        Assert.That(result.ResponseTarget, Is.EqualTo(ResponseTarget.Source));
        _badgeRepoMock.Verify(repo =>
                repo.AddBadge(recipient.Id, species, Badge.BadgeSource.ManualCreation, null),
            Times.Exactly(123));
    }
}
