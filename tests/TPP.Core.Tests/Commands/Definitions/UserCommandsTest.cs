using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NSubstitute;
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
    ) => new(
        id: Guid.NewGuid().ToString(),
        name: name, twitchDisplayName: twitchDisplayName ?? "☺" + name, simpleName: name.ToLower(), color: null,
        firstActiveAt: Instant.FromUnixTimeSeconds(0),
        lastActiveAt: Instant.FromUnixTimeSeconds(0),
        lastMessageAt: null,
        pokeyen: pokeyen, tokens: tokens,
        pokeyenBetRank: pokeyenBetRank, glowColorUnlocked: glowColorUnlocked,
        participationEmblems: emblems);

    private static Message MockMessage(User user, string text = "") =>
        new(user, text, new MessageSource.PrimaryChat(), string.Empty);

    private IUserRepo _userRepoMock = null!;
    private IBank<User> _pokeyenBankMock = null!;
    private IBank<User> _tokenBankMock = null!;
    private IMessageSender _messageSenderMock = null!;
    private ArgsParser _argsParser = null!;

    private UserCommands _userCommands = null!;
    private CurrencyCommands _currencyCommands = null!;
    private CosmeticsCommands _cosmeticsCommands = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepoMock = Substitute.For<IUserRepo>();
        _pokeyenBankMock = Substitute.For<IBank<User>>();
        _tokenBankMock = Substitute.For<IBank<User>>();
        _messageSenderMock = Substitute.For<IMessageSender>();
        _userCommands = new UserCommands(userRepo: _userRepoMock);
        _currencyCommands = new CurrencyCommands(
            pokeyenBank: _pokeyenBankMock,
            tokenBank: _tokenBankMock,
            messageSender: _messageSenderMock);
        _cosmeticsCommands = new CosmeticsCommands(userRepo: _userRepoMock, tokenBank: _tokenBankMock);
        _argsParser = new ArgsParser();
        _argsParser.AddArgumentParser(new OptionalParser(_argsParser));
        _argsParser.AddArgumentParser(new UserParser(_userRepoMock));
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
        _pokeyenBankMock.GetAvailableMoney(user).Returns(900);
        _pokeyenBankMock.GetReservedMoney(user).Returns(100);
        _tokenBankMock.GetAvailableMoney(user).Returns(9);
        _tokenBankMock.GetReservedMoney(user).Returns(1);

        CommandResult result = await _currencyCommands.CheckBalance(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(result.Response, Is.EqualTo("You have P900 pokeyen (P100 reserved) and T9 tokens (T1 reserved)."));
    }

    [Test]
    public async Task TestBalanceSelfWithRank()
    {
        var user = MockUser(pokeyen: 1000, tokens: 10, pokeyenBetRank: 123);
        _pokeyenBankMock.GetAvailableMoney(user).Returns(1000);
        _pokeyenBankMock.GetReservedMoney(user).Returns(0);
        _tokenBankMock.GetAvailableMoney(user).Returns(10);
        _tokenBankMock.GetReservedMoney(user).Returns(0);

        CommandResult result = await _currencyCommands.CheckBalance(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(
            result.Response, Is.EqualTo("You have P1000 pokeyen and T10 tokens. " +
                                        "You are currently rank 123 in the leaderboard."));
    }

    [Test]
    public async Task TestBalanceOther()
    {
        var user = MockUser(pokeyen: 1000, tokens: 10, pokeyenBetRank: 123);
        _pokeyenBankMock.GetAvailableMoney(user).Returns(900);
        _pokeyenBankMock.GetReservedMoney(user).Returns(100);
        _tokenBankMock.GetAvailableMoney(user).Returns(9);
        _tokenBankMock.GetReservedMoney(user).Returns(1);
        _userRepoMock.FindBySimpleName(user.SimpleName)
            .Returns(user);

        var message = new Message(MockUser("Someone_Else"), "", new MessageSource.PrimaryChat(), string.Empty);
        CommandResult result = await _currencyCommands.CheckBalance(new CommandContext(message,
            ImmutableList.Create(user.Name), _argsParser));

        Assert.That(
            result.Response, Is.EqualTo($"{user.Name} has P1000 pokeyen and T10 tokens. " +
                                        "They are currently rank 123 in the leaderboard."));
    }

    [Test]
    public async Task TestSetGlowNotUnlocked()
    {
        var user = MockUser(glowColorUnlocked: false);
        CommandResult result = await _cosmeticsCommands.SetGlow(new CommandContext(MockMessage(user),
            ImmutableList.Create("#123456"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("glow color is still locked, use 'unlockglow' to unlock (costs T1)"));
        Assert.That(_userRepoMock.ReceivedCalls().Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task TestSetGlow()
    {
        var user = MockUser(glowColorUnlocked: true);
        const string glowColor = "123456";

        CommandResult result = await _cosmeticsCommands.SetGlow(new CommandContext(MockMessage(user),
            ImmutableList.Create('#' + glowColor), _argsParser));

        Assert.That(result.Response, Is.EqualTo("glow color set to #123456"));
        await _userRepoMock.Received(1).SetGlowColor(user, glowColor);
    }

    [Test]
    public async Task TestRemoveGlow()
    {
        var user = MockUser();

        CommandResult result = await _cosmeticsCommands.RemoveGlow(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(result.Response, Is.EqualTo("your glow color was removed"));
        await _userRepoMock.Received(1).SetGlowColor(user, null);
    }

    [Test]
    public async Task TestUnlockGlowAlreadyUnlocked()
    {
        var user = MockUser(glowColorUnlocked: true);

        CommandResult result = await _cosmeticsCommands.UnlockGlow(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(result.Response, Is.EqualTo("glow color is already unlocked"));
        Assert.That(_userRepoMock.ReceivedCalls().Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task TestUnlockGlowNotEnoughTokens()
    {
        var user = MockUser(glowColorUnlocked: false);
        _tokenBankMock.GetAvailableMoney(user).Returns(0);

        CommandResult result = await _cosmeticsCommands.UnlockGlow(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(result.Response, Is.EqualTo("you don't have T1 to unlock the glow color"));
        Assert.That(_userRepoMock.ReceivedCalls().Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task TestUnlockGlow()
    {
        var user = MockUser(glowColorUnlocked: false);
        _tokenBankMock.GetAvailableMoney(user).Returns(1);

        CommandResult result = await _cosmeticsCommands.UnlockGlow(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(result.Response, Is.EqualTo("your glow color was unlocked"));
        await _userRepoMock.Received(1).SetGlowColorUnlocked(user, true);
        await _tokenBankMock.Received(1).PerformTransaction(
                Arg.Is((Transaction<User> tx) =>
                    tx.Change == -1 && tx.User == user && tx.Type == TransactionType.SecondaryColorUnlock),
                Arg.Any<CancellationToken>());
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
        Assert.That(_userRepoMock.ReceivedCalls().Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task TestSetDisplaynameNameWithSpecialChars()
    {
        var user = MockUser("User", twitchDisplayName: "ユーザー名");
        string newDisplayName = user.SimpleName.ToUpper();

        CommandResult result = await _userCommands.SetDisplayName(new CommandContext(MockMessage(user),
            ImmutableList.Create(newDisplayName), _argsParser));

        Assert.That(result.Response, Is.EqualTo($"your display name has been updated to '{newDisplayName}'"));
        await _userRepoMock.Received(1).SetDisplayName(user, newDisplayName);
    }

    [Test]
    public async Task TestCheckEmblems()
    {
        var user = MockUser(emblems: new SortedSet<int>([1, 2, 47]));

        CommandResult result = await _cosmeticsCommands.CheckEmblems(new CommandContext(MockMessage(user),
            ImmutableList<string>.Empty, _argsParser));

        Assert.That(
            result.Response, Is.EqualTo("you have participated in the following runs: #1 (Red), #2 (Crystal), #47 (Sirius)"));
    }

    [Test]
    public async Task TestCheckEmblemsSomeoneElse()
    {
        var userSelf = MockUser(emblems: new SortedSet<int>([3, 4, 9]));
        var userOther = MockUser(name: "OtherUser", emblems: new SortedSet<int>([1, 2, 47]));
        _userRepoMock
            .FindBySimpleName(userOther.SimpleName)
            .Returns(userOther);

        CommandResult result = await _cosmeticsCommands.CheckEmblems(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("oThErUsEr"), _argsParser));

        Assert.That(
            result.Response, Is.EqualTo("OtherUser has participated in the following runs: #1 (Red), #2 (Crystal), #47 (Sirius)"));
    }

    [Test]
    public async Task TestSelectEmblemNotOwned()
    {
        var user = MockUser(emblems: new SortedSet<int>([1, 2, 47]));

        CommandResult result = await _cosmeticsCommands.SelectEmblem(new CommandContext(MockMessage(user),
            ImmutableList.Create("10"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("you don't own that participation badge"));
        Assert.That(_userRepoMock.ReceivedCalls().Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task TestSelectEmblem()
    {
        var user = MockUser(emblems: new SortedSet<int>([1, 2, 47]));

        CommandResult result = await _cosmeticsCommands.SelectEmblem(new CommandContext(MockMessage(user),
            ImmutableList.Create("2"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("color of participation badge #2 (Crystal) successfully equipped"));
        await _userRepoMock.Received(1).SetSelectedEmblem(user, 2);
    }

    [Test]
    public async Task TestDonateTokens()
    {
        User userSelf = MockUser("Self");
        User userRecipient = MockUser("Recipient");
        _userRepoMock.FindBySimpleName(userRecipient.SimpleName)
            .Returns(Task.FromResult((User?)userRecipient));
        _tokenBankMock.GetAvailableMoney(userSelf).Returns(Task.FromResult(1L));
        List<Transaction<User>> transactions = [];
        _tokenBankMock.PerformTransactions(
            Arg.Do<IEnumerable<Transaction<User>>>(txs => transactions.AddRange(txs)),
            Arg.Any<CancellationToken>()
        ).ReturnsForAnyArgs(new List<TransactionLog>());

        CommandResult result = await _currencyCommands.Donate(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("T1", "Recipient"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("You successfully donated T1 to @Recipient!"));
        Assert.That(transactions, Is.EquivalentTo(new List<Transaction<User>>
        {
            new(userSelf, -1, "donation_give"),
            new(userRecipient, 1, "donation_recieve"),
        }));
    }

    [Test]
    public async Task TestDonateTokensInsufficientFunds()
    {
        User userSelf = MockUser("Self");
        User userRecipient = MockUser("Recipient");
        _userRepoMock.FindBySimpleName(userRecipient.SimpleName)
            .Returns(Task.FromResult((User?)userRecipient));
        _tokenBankMock.GetAvailableMoney(userSelf).Returns(Task.FromResult(1L));

        CommandResult result = await _currencyCommands.Donate(new CommandContext(MockMessage(userSelf),
            ImmutableList.Create("T2", "Recipient"), _argsParser));

        Assert.That(result.Response, Is.EqualTo("You are trying to donate T2 but you only have T1."));
        await _tokenBankMock.DidNotReceive()
            .PerformTransactions(Arg.Any<IEnumerable<Transaction<User>>>(), Arg.Any<CancellationToken>());
    }
}
