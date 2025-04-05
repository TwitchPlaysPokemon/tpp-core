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
using TPP.Inputting;
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
        new(user, text, new MessageSource.PrimaryChat(), string.Empty);

    private IUserRepo _userRepoMock = null!;
    private ArgsParser _argsParser = null!;
    private IBank<User> _pokeyenBankMock = null!;
    private IBank<User> _tokensBankMock = null!;
    private IMessageSender _messageSenderMock = null!;
    private IBadgeRepo _badgeRepoMock = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepoMock = Substitute.For<IUserRepo>();
        _argsParser = new ArgsParser();
        _argsParser.AddArgumentParser(new StringParser());
        _argsParser.AddArgumentParser(new SignedPokeyenParser());
        _argsParser.AddArgumentParser(new SignedTokensParser());
        _argsParser.AddArgumentParser(new AnyOrderParser(_argsParser));
        _argsParser.AddArgumentParser(new OptionalParser(_argsParser));
        _argsParser.AddArgumentParser(new ManyOfParser(_argsParser));
        _argsParser.AddArgumentParser(new RoleParser());
        _argsParser.AddArgumentParser(new UserParser(_userRepoMock));
        _argsParser.AddArgumentParser(new PositiveIntParser());
        _pokeyenBankMock = Substitute.For<IBank<User>>();
        _tokensBankMock = Substitute.For<IBank<User>>();
        _messageSenderMock = Substitute.For<IMessageSender>();
        _badgeRepoMock = Substitute.For<IBadgeRepo>();
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
        _userRepoMock.FindBySimpleName(user.SimpleName).Returns(user);
        OperatorCommands operatorCommands = new OperatorCommands(
            new ToggleableStopToken(), new MuteInputsToken(),
            _tokensBankMock, _tokensBankMock,
            _messageSenderMock, _badgeRepoMock,
            _userRepoMock, Substitute.For<IInputSidePicksRepo>(), SystemClock.Instance);

        {
            CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(user),
                ImmutableList.Create(user.Name, "T123", "because", "reason"), _argsParser));
            Assert.That(result.Response, Is.EqualTo("Your token balance was adjusted by +123. Reason: because reason"));

            var transaction = new Transaction<User>(user, 123, "manual_adjustment",
                new Dictionary<string, object?> { ["responsible_user"] = user.Id });
            await _tokensBankMock.Received(1).PerformTransaction(
                Arg.Is<Transaction<User>>(tx => AreTransactionsEqual(tx, transaction)), default);
        }
        {
            CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(user),
                ImmutableList.Create(user.Name, "T-123", "because"), _argsParser));
            Assert.That(result.Response, Is.EqualTo("Your token balance was adjusted by -123. Reason: because"));

            var transaction = new Transaction<User>(user, -123, "manual_adjustment",
                new Dictionary<string, object?> { ["responsible_user"] = user.Id });
            await _tokensBankMock.Received(1).PerformTransaction(
                Arg.Is<Transaction<User>>(tx => AreTransactionsEqual(tx, transaction)), default);
        }
        Assert.That(_tokensBankMock.ReceivedCalls().Count(), Is.EqualTo(2));
        Assert.That(_pokeyenBankMock.ReceivedCalls().Count(), Is.EqualTo(0));
        Assert.That(_messageSenderMock.ReceivedCalls().Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task adjust_tokens_someone_else()
    {
        User userSelf = MockOperator("MockUserSelf");
        User userOther = MockUser("MockUserOther");
        _userRepoMock.FindBySimpleName(userOther.SimpleName).Returns(userOther);
        OperatorCommands operatorCommands = new OperatorCommands(
            new ToggleableStopToken(), new MuteInputsToken(),
            _tokensBankMock, _tokensBankMock,
            _messageSenderMock, _badgeRepoMock,
            _userRepoMock, Substitute.For<IInputSidePicksRepo>(), SystemClock.Instance);

        {
            CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(userSelf),
                ImmutableList.Create(userOther.Name, "T123", "because", "reason"), _argsParser));
            Assert.That(result.Response, Is.EqualTo("MockUserOther's token balance was adjusted by +123. Reason: because reason"));

            var transaction = new Transaction<User>(userOther, 123, "manual_adjustment",
                new Dictionary<string, object?> { ["responsible_user"] = userSelf.Id });
            await _tokensBankMock.Received(1).PerformTransaction(
                Arg.Is<Transaction<User>>(tx => AreTransactionsEqual(tx, transaction)), default);

            const string notification = "MockUserSelf adjusted your token balance by +123. Reason: because reason";
            await _messageSenderMock.Received(1).SendWhisper(userOther, notification);
        }
        {
            CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(userSelf),
                ImmutableList.Create(userOther.Name, "T-123", "because"), _argsParser));
            Assert.That(result.Response, Is.EqualTo("MockUserOther's token balance was adjusted by -123. Reason: because"));

            var transaction = new Transaction<User>(userOther, -123, "manual_adjustment",
                new Dictionary<string, object?> { ["responsible_user"] = userSelf.Id });
            await _tokensBankMock.Received(1).PerformTransaction(
                Arg.Is<Transaction<User>>(tx => AreTransactionsEqual(tx, transaction)), default);

            const string notification = "MockUserSelf adjusted your token balance by -123. Reason: because";
            await _messageSenderMock.Received(1).SendWhisper(userOther, notification);
        }
        Assert.That(_tokensBankMock.ReceivedCalls().Count(), Is.EqualTo(2));
        Assert.That(_pokeyenBankMock.ReceivedCalls().Count(), Is.EqualTo(0));
        Assert.That(_messageSenderMock.ReceivedCalls().Count(), Is.EqualTo(2));
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
            new ToggleableStopToken(), new MuteInputsToken(),
            _tokensBankMock, _tokensBankMock,
            _messageSenderMock, _badgeRepoMock,
            _userRepoMock, Substitute.For<IInputSidePicksRepo>(), SystemClock.Instance);
        _userRepoMock.FindBySimpleName("gifter").Returns(Task.FromResult((User?)gifter));
        _userRepoMock.FindBySimpleName("recipient").Returns(Task.FromResult((User?)recipient));
        Badge badge1 = new("badge1", gifter.Id, species, Badge.BadgeSource.ManualCreation, Instant.MinValue);
        Badge badge2 = new("badge2", gifter.Id, species, Badge.BadgeSource.ManualCreation, Instant.MinValue);
        Badge badge3 = new("badge3", gifter.Id, species, Badge.BadgeSource.ManualCreation, Instant.MinValue);
        _badgeRepoMock.FindByUserAndSpecies(gifter.Id, species, 2)
            .Returns(ImmutableList.Create(badge1, badge2, badge3));

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
        await _badgeRepoMock.Received(1).TransferBadges(
            Arg.Is<IImmutableList<Badge>>(list => list.SequenceEqual(ImmutableList.Create(badge1, badge2))),
            recipient.Id,
            "gift_remote",
            Arg.Is<IDictionary<string, object?>>(dict => dict.DictionaryEqual(data)));
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
            new ToggleableStopToken(), new MuteInputsToken(),
            _tokensBankMock, _tokensBankMock,
            _messageSenderMock, _badgeRepoMock,
            _userRepoMock, Substitute.For<IInputSidePicksRepo>(), SystemClock.Instance);
        _userRepoMock.FindBySimpleName("gifter").Returns(Task.FromResult((User?)gifter));
        _userRepoMock.FindBySimpleName("recipient").Returns(Task.FromResult((User?)recipient));

        CommandResult result = await operatorCommands.TransferBadge(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("gifter", "recipient", "species", "2"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("Must provide a reason"));
        await _badgeRepoMock.DidNotReceive().TransferBadges(
            Arg.Any<IImmutableList<Badge>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IDictionary<string, object?>>());
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
            new ToggleableStopToken(), new MuteInputsToken(),
            _tokensBankMock, _tokensBankMock,
            _messageSenderMock, _badgeRepoMock,
            _userRepoMock, Substitute.For<IInputSidePicksRepo>(), SystemClock.Instance);
        _userRepoMock.FindBySimpleName("gifter").Returns(Task.FromResult((User?)gifter));
        _userRepoMock.FindBySimpleName("recipient").Returns(Task.FromResult((User?)recipient));
        _badgeRepoMock.FindByUserAndSpecies(gifter.Id, species, 1)
            .Returns(ImmutableList<Badge>.Empty);

        CommandResult result = await operatorCommands.TransferBadge(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("gifter", "recipient", "species", "reason"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("You tried to transfer 1 #001 species badges, but the gifter only has 0."));
        Assert.That(result.ResponseTarget, Is.EqualTo(ResponseTarget.Source));
        await _badgeRepoMock.DidNotReceive().TransferBadges(
            Arg.Any<IImmutableList<Badge>>(),
            Arg.Any<string?>(),
            Arg.Any<string>(),
            Arg.Any<IDictionary<string, object?>>());
    }

    [Test]
    public async Task TestCreateBadge()
    {
        PkmnSpecies species = PkmnSpecies.RegisterName("1", "Species");
        _argsParser.AddArgumentParser(new PkmnSpeciesParser(new[] { species }));
        User user = MockOperator("MockUser");
        User recipient = MockUser("Recipient");
        OperatorCommands operatorCommands = new(
            new ToggleableStopToken(), new MuteInputsToken(),
            _tokensBankMock, _tokensBankMock,
            _messageSenderMock, _badgeRepoMock,
            _userRepoMock, Substitute.For<IInputSidePicksRepo>(), SystemClock.Instance);
        _userRepoMock.FindBySimpleName("recipient").Returns(Task.FromResult((User?)recipient));

        CommandResult result = await operatorCommands.CreateBadge(new CommandContext(MockMessage(user),
            ImmutableList.Create("recipient", "species", "123"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("123 badges of species #001 Species created for user Recipient."));
        Assert.That(result.ResponseTarget, Is.EqualTo(ResponseTarget.Source));
        await _badgeRepoMock.Received(123).AddBadge(recipient.Id, species, Badge.BadgeSource.ManualCreation, null);
    }
}
