using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using ArgsParsing;
using ArgsParsing.TypeParsers;
using Core.Commands;
using Core.Commands.Definitions;
using Moq;
using NodaTime;
using NUnit.Framework;
using Persistence.Models;
using Persistence.Repos;

namespace Core.Tests.Commands.Definitions
{
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
            name: name, twitchDisplayName: twitchDisplayName ?? name, simpleName: name.ToLower(), color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0),
            lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null,
            pokeyen: pokeyen, tokens: tokens,
            pokeyenBetRank: pokeyenBetRank, glowColorUnlocked: glowColorUnlocked,
            participationEmblems: emblems);

        private static Message MockMessage(User user, string text = "") => new Message(user, text, MessageSource.Chat);

        private Mock<IUserRepo> _userRepoMock = null!;
        private Mock<IBank<User>> _pokeyenBankMock = null!;
        private Mock<IBank<User>> _tokenBankMock = null!;
        private ArgsParser _argsParser = null!;

        private UserCommands _userCommands = null!;

        [SetUp]
        public void SetUp()
        {
            _userRepoMock = new Mock<IUserRepo>();
            _pokeyenBankMock = new Mock<IBank<User>>();
            _tokenBankMock = new Mock<IBank<User>>();
            _userCommands = new UserCommands(
                userRepo: _userRepoMock.Object,
                pokeyenBank: _pokeyenBankMock.Object,
                tokenBank: _tokenBankMock.Object);
            _argsParser = new ArgsParser();
            _argsParser.AddArgumentParser(new OptionalParser(_argsParser));
            _argsParser.AddArgumentParser(new UserParser(_userRepoMock.Object));
            _argsParser.AddArgumentParser(new HexColorParser());
            _argsParser.AddArgumentParser(new StringParser());
            _argsParser.AddArgumentParser(new IntParser());
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
                ImmutableList.Create<string>(), _argsParser));

            Assert.AreEqual("You have P900 pokeyen (P100 reserved) and T9 tokens (T1 reserved).", result.Response);
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
                ImmutableList.Create<string>(), _argsParser));

            Assert.AreEqual(
                "You have P1000 pokeyen and T10 tokens. " +
                "You are currently rank 123 in the leaderboard.",
                result.Response);
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

            var message = new Message(MockUser("someone_else"), "", MessageSource.Chat);
            CommandResult result = await _userCommands.CheckBalance(new CommandContext(message,
                ImmutableList.Create(user.Name), _argsParser));

            Assert.AreEqual(
                $"{user.TwitchDisplayName} has P1000 pokeyen and T10 tokens. " +
                "They are currently rank 123 in the leaderboard.",
                result.Response);
        }

        [Test]
        public async Task TestSetGlowNotUnlocked()
        {
            var user = MockUser(glowColorUnlocked: false);
            CommandResult result = await _userCommands.SetGlow(new CommandContext(MockMessage(user),
                ImmutableList.Create("#123456"), _argsParser));

            Assert.AreEqual("glow color is still locked, use 'unlockglow' to unlock (costs T1)", result.Response);
            _userRepoMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task TestSetGlow()
        {
            var user = MockUser(glowColorUnlocked: true);
            string glowColor = "123456";

            CommandResult result = await _userCommands.SetGlow(new CommandContext(MockMessage(user),
                ImmutableList.Create('#' + glowColor), _argsParser));

            Assert.AreEqual("glow color set to #123456", result.Response);
            _userRepoMock.Verify(u => u.SetGlowColor(user, glowColor), Times.Once);
        }

        [Test]
        public async Task TestRemoveGlow()
        {
            var user = MockUser();

            CommandResult result = await _userCommands.RemoveGlow(new CommandContext(MockMessage(user),
                ImmutableList.Create<string>(), _argsParser));

            Assert.AreEqual("your glow color was removed", result.Response);
            _userRepoMock.Verify(u => u.SetGlowColor(user, null), Times.Once);
        }

        [Test]
        public async Task TestUnlockGlowAlreadyUnlocked()
        {
            var user = MockUser(glowColorUnlocked: true);

            CommandResult result = await _userCommands.UnlockGlow(new CommandContext(MockMessage(user),
                ImmutableList.Create<string>(), _argsParser));

            Assert.AreEqual("glow color is already unlocked", result.Response);
            _userRepoMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task TestUnlockGlowNotEnoughTokens()
        {
            var user = MockUser(glowColorUnlocked: false);
            _tokenBankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(0);

            CommandResult result = await _userCommands.UnlockGlow(new CommandContext(MockMessage(user),
                ImmutableList.Create<string>(), _argsParser));

            Assert.AreEqual("you don't have T1 to unlock the glow color", result.Response);
            _userRepoMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task TestUnlockGlow()
        {
            var user = MockUser(glowColorUnlocked: false);
            _tokenBankMock.Setup(b => b.GetAvailableMoney(user)).ReturnsAsync(1);

            CommandResult result = await _userCommands.UnlockGlow(new CommandContext(MockMessage(user),
                ImmutableList.Create<string>(), _argsParser));

            Assert.AreEqual("your glow color was unlocked", result.Response);
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
            var user = MockUser("user", twitchDisplayName: "user");
            string newDisplayName = user.SimpleName.ToUpper();

            CommandResult result = await _userCommands.SetDisplayName(new CommandContext(MockMessage(user),
                ImmutableList.Create(newDisplayName), _argsParser));

            Assert.AreEqual(
                "you don't have any special characters in your name " +
                "and can therefore still change it in your twitch settings", result.Response);
            _userRepoMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task TestSetDisplaynameNameWithSpecialChars()
        {
            var user = MockUser("user", twitchDisplayName: "ユーザー名");
            string newDisplayName = user.SimpleName.ToUpper();

            CommandResult result = await _userCommands.SetDisplayName(new CommandContext(MockMessage(user),
                ImmutableList.Create(newDisplayName), _argsParser));

            Assert.AreEqual($"your display name has been updated to '{newDisplayName}'", result.Response);
            _userRepoMock.Verify(u => u.SetDisplayName(user, newDisplayName), Times.Once);
        }

        [Test]
        public async Task TestCheckEmblems()
        {
            var user = MockUser(emblems: new SortedSet<int>(new[] { 1, 2, 47 }));

            CommandResult result = await _userCommands.CheckEmblems(new CommandContext(MockMessage(user),
                ImmutableList.Create<string>(), _argsParser));

            Assert.AreEqual(
                "you have participated in the following runs: #1 (Red), #2 (Crystal), #47 (Sirius)",
                result.Response);
        }

        [Test]
        public async Task TestSelectEmblemNotOwned()
        {
            var user = MockUser(emblems: new SortedSet<int>(new[] { 1, 2, 47 }));

            CommandResult result = await _userCommands.SelectEmblem(new CommandContext(MockMessage(user),
                ImmutableList.Create("10"), _argsParser));

            Assert.AreEqual("you don't own that participation badge", result.Response);
            _userRepoMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task TestSelectEmblem()
        {
            var user = MockUser(emblems: new SortedSet<int>(new[] { 1, 2, 47 }));

            CommandResult result = await _userCommands.SelectEmblem(new CommandContext(MockMessage(user),
                ImmutableList.Create("2"), _argsParser));

            Assert.AreEqual("color of participation badge #2 (Crystal) successfully equipped", result.Response);
            _userRepoMock.Verify(u => u.SetSelectedEmblem(user, 2), Times.Once);
        }
    }
}
