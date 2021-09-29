using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.ArgsParsing;
using TPP.ArgsParsing.TypeParsers;
using TPP.Core.Chat;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests.Commands.Definitions;

public class UserCommandsTest
{
    private static User MockUser(
        string name = "user",
        int pokeyen = 0,
        int tokens = 0,
        string? twitchDisplayName = null,
        int? pokeyenBetRank = null,
        bool glowColorUnlocked = false,
        SortedSet<int>? emblems = null
    ) => new User(
        id: Guid.NewGuid().ToString(),
        name: name, twitchDisplayName: twitchDisplayName ?? "☺" + name, simpleName: name.ToLower(), color: null,
        firstActiveAt: Instant.FromUnixTimeSeconds(0),
        lastActiveAt: Instant.FromUnixTimeSeconds(0),
        lastMessageAt: null,
        pokeyen: pokeyen, tokens: tokens,
        pokeyenBetRank: pokeyenBetRank, glowColorUnlocked: glowColorUnlocked,
        participationEmblems: emblems);

    private static Message MockMessage(User user, string text = "")
        => new Message(user, text, MessageSource.Chat, string.Empty);

    private Mock<IUserRepo> _userRepoMock = null!;
    private Mock<IBank<User>> _pokeyenBankMock = null!;
    private Mock<IBank<User>> _tokenBankMock = null!;
    private Mock<IMessageSender> _messageSenderMock = null!;
    private ArgsParser _argsParser = null!;

    private UserCommands _userCommands = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepoMock = new Mock<IUserRepo>();
        _pokeyenBankMock = new Mock<IBank<User>>();
        _tokenBankMock = new Mock<IBank<User>>();
        _messageSenderMock = new Mock<IMessageSender>();
        _userCommands = new UserCommands(
            userRepo: _userRepoMock.Object,
            pokeyenBank: _pokeyenBankMock.Object,
            tokenBank: _tokenBankMock.Object,
            messageSender: _messageSenderMock.Object);
        _argsParser = new ArgsParser();
        _argsParser.AddArgumentParser(new OptionalParser(_argsParser));
        _argsParser.AddArgumentParser(new UserParser(_userRepoMock.Object));
        _argsParser.AddArgumentParser(new HexColorParser());
        _argsParser.AddArgumentParser(new StringParser());
        _argsParser.AddArgumentParser(new NonNegativeIntParser());
        _argsParser.AddArgumentParser(new AnyOrderParser(_argsParser));
        _argsParser.AddArgumentParser(new TokensParser());
    }

    [Test]
    public async Task TestBalanceSelfWithReserved()
    {
        var user = MockUser(pokeyen: 1000, tokens: 10);
        _pokeyenBankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(900);
        _pokeyenBankMock.Setup(b => b.GetReservedMoney(user)).ReturnsAsync(100);
        _tokenBankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(9);
        _tokenBankMock.Setup(b => b.GetReservedMoney(user)).ReturnsAsync(1);

        CommandResult result = await _userCommands.CheckBalance(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(result.Response, Is.EqualTo("You have P900 pokeyen (P100 reserved) and T9 tokens (T1 reserved)."));
    }

    [Test]
    public async Task TestBalanceSelfWithRank()
    {
        var user = MockUser(pokeyen: 1000, tokens: 10, pokeyenBetRank: 123);
        _pokeyenBankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(1000);
        _pokeyenBankMock.Setup(b => b.GetReservedMoney(user)).ReturnsAsync(0);
        _tokenBankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(10);
        _tokenBankMock.Setup(b => b.GetReservedMoney(user)).ReturnsAsync(0);

        CommandResult result = await _userCommands.CheckBalance(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(
            result.Response, Is.EqualTo("You have P1000 pokeyen and T10 tokens. " +
                                        "You are currently rank 123 in the leaderboard."));
    }

    [Test]
    public async Task TestBalanceOther()
    {
        var user = MockUser(pokeyen: 1000, tokens: 10, pokeyenBetRank: 123);
        _pokeyenBankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(900);
        _pokeyenBankMock.Setup(b => b.GetReservedMoney(user)).ReturnsAsync(100);
        _tokenBankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(9);
        _tokenBankMock.Setup(b => b.GetReservedMoney(user)).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.FindBySimpleName(user.SimpleName))
            .ReturnsAsync(user);

        var message = new Message(MockUser("Someone_Else"), "", MessageSource.Chat, string.Empty);
        CommandResult result = await _userCommands.CheckBalance(new CommandContext(message,
            ImmutableList.Create(user.Name), _argsParser));

        Assert.That(
            result.Response, Is.EqualTo($"{user.Name} has P1000 pokeyen and T10 tokens. " +
                                        "They are currently rank 123 in the leaderboard."));
    }

    [Test]
    public async Task TestSetGlowNotUnlocked()
    {
        var user = MockUser(glowColorUnlocked: false);
        CommandResult result = await _userCommands.SetGlow(new CommandContext(MockMessage(user),
            ImmutableList.Create("#123456"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("glow color is still locked, use 'unlockglow' to unlock (costs T1)"));
        _userRepoMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task TestSetGlow()
    {
        var user = MockUser(glowColorUnlocked: true);
        const string glowColor = "123456";

        CommandResult result = await _userCommands.SetGlow(new CommandContext(MockMessage(user),
            ImmutableList.Create('#' + glowColor), _argsParser));

        Assert.That(result.Response, Is.EqualTo("glow color set to #123456"));
        _userRepoMock.Verify(u => u.SetGlowColor(user, glowColor), Times.Once);
    }

    [Test]
    public async Task TestRemoveGlow()
    {
        var user = MockUser();

        CommandResult result = await _userCommands.RemoveGlow(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(result.Response, Is.EqualTo("your glow color was removed"));
        _userRepoMock.Verify(u => u.SetGlowColor(user, null), Times.Once);
    }

    [Test]
    public async Task TestUnlockGlowAlreadyUnlocked()
    {
        var user = MockUser(glowColorUnlocked: true);

        CommandResult result = await _userCommands.UnlockGlow(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(result.Response, Is.EqualTo("glow color is already unlocked"));
        _userRepoMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task TestUnlockGlowNotEnoughTokens()
    {
        var user = MockUser(glowColorUnlocked: false);
        _tokenBankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(0);

        CommandResult result = await _userCommands.UnlockGlow(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(result.Response, Is.EqualTo("you don't have T1 to unlock the glow color"));
        _userRepoMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task TestUnlockGlow()
    {
        var user = MockUser(glowColorUnlocked: false);
        _tokenBankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(1);

        CommandResult result = await _userCommands.UnlockGlow(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(result.Response, Is.EqualTo("your glow color was unlocked"));
        _userRepoMock.Verify(u => u.SetGlowColorUnlocked(user, true), Times.Once);
        _tokenBankMock.Verify(b => b.PerformTransaction(
                It.Is((Transaction<User> tx) =>
                    tx.Change == -1 && tx.User == user && tx.Type == TransactionType.SecondaryColorUnlock),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task TestSetDisplaynameSimpleName()
    {
        var user = MockUser("User", twitchDisplayName: "user");
        string newDisplayName = user.SimpleName.ToUpper();

        CommandResult result = await _userCommands.SetDisplayName(new CommandContext(MockMessage(user),
            ImmutableList.Create(newDisplayName), _argsParser));

        Assert.That(
            result.Response, Is.EqualTo("you don't have any special characters in your name " +
                                        "and can therefore still change it in your twitch settings"));
        _userRepoMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task TestSetDisplaynameNameWithSpecialChars()
    {
        var user = MockUser("User", twitchDisplayName: "ユーザー名");
        string newDisplayName = user.SimpleName.ToUpper();

        CommandResult result = await _userCommands.SetDisplayName(new CommandContext(MockMessage(user),
            ImmutableList.Create(newDisplayName), _argsParser));

        Assert.That(result.Response, Is.EqualTo($"your display name has been updated to '{newDisplayName}'"));
        _userRepoMock.Verify(u => u.SetDisplayName(user, newDisplayName), Times.Once);
    }

    [Test]
    public async Task TestCheckEmblems()
    {
        var user = MockUser(emblems: new SortedSet<int>(new[] { 1, 2, 47 }));

        CommandResult result = await _userCommands.CheckEmblems(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(
            result.Response, Is.EqualTo("you have participated in the following runs: #1 (Red), #2 (Crystal), #47 (Sirius)"));
    }

    [Test]
    public async Task TestCheckEmblemsSomeoneElse()
    {
        var userSelf = MockUser(emblems: new SortedSet<int>(new[] { 3, 4, 9 }));
        var userOther = MockUser(name: "OtherUser", emblems: new SortedSet<int>(new[] { 1, 2, 47 }));
        _userRepoMock
            .Setup(repo => repo.FindBySimpleName(userOther.SimpleName))
            .ReturnsAsync(userOther);

        CommandResult result = await _userCommands.CheckEmblems(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("oThErUsEr"), _argsParser));

        Assert.That(
            result.Response, Is.EqualTo("OtherUser has participated in the following runs: #1 (Red), #2 (Crystal), #47 (Sirius)"));
    }

    [Test]
    public async Task TestSelectEmblemNotOwned()
    {
        var user = MockUser(emblems: new SortedSet<int>(new[] { 1, 2, 47 }));

        CommandResult result = await _userCommands.SelectEmblem(new CommandContext(MockMessage(user),
            ImmutableList.Create("10"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("you don't own that participation badge"));
        _userRepoMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task TestSelectEmblem()
    {
        var user = MockUser(emblems: new SortedSet<int>(new[] { 1, 2, 47 }));

        CommandResult result = await _userCommands.SelectEmblem(new CommandContext(MockMessage(user),
            ImmutableList.Create("2"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("color of participation badge #2 (Crystal) successfully equipped"));
        _userRepoMock.Verify(u => u.SetSelectedEmblem(user, 2), Times.Once);
    }

    [Test]
    public async Task TestDonateTokens()
    {
        User userSelf = MockUser("Self");
        User userRecipient = MockUser("Recipient");
        _userRepoMock.Setup(r => r.FindBySimpleName(userRecipient.SimpleName))
            .Returns(Task.FromResult((User?)userRecipient));
        _tokenBankMock.Setup(b => b.GetAvailableMoney(userSelf)).Returns(Task.FromResult(1L));
        List<IEnumerable<Transaction<User>>> txInvocations = new();
        _tokenBankMock.Setup(b => b.PerformTransactions(Capture.In(txInvocations), It.IsAny<CancellationToken>()));

        CommandResult result = await _userCommands.Donate(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("T1", "Recipient"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("has donated T1 to @Recipient!"));
        List<Transaction<User>> txs = txInvocations.SelectMany(t => t).ToList();
        CollectionAssert.AreEquivalent(new List<Transaction<User>>
        {
            new(userSelf, -1, "donation_give"),
            new(userRecipient, 1, "donation_recieve"),
        }, txs);
    }

    [Test]
    public async Task TestDonateTokensInsufficientFunds()
    {
        User userSelf = MockUser("Self");
        User userRecipient = MockUser("Recipient");
        _userRepoMock.Setup(r => r.FindBySimpleName(userRecipient.SimpleName))
            .Returns(Task.FromResult((User?)userRecipient));
        _tokenBankMock.Setup(b => b.GetAvailableMoney(userSelf)).Returns(Task.FromResult(1L));

        CommandResult result = await _userCommands.Donate(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("T2", "Recipient"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("You are trying to donate T2 but you only have T1."));
        _tokenBankMock.Verify(b =>
                b.PerformTransactions(It.IsAny<IEnumerable<Transaction<User>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
