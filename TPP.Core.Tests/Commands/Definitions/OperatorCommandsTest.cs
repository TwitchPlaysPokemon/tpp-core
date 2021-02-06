using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.Core.Tests.Commands.Definitions
{
    public class OperatorCommandsTest
    {
        private static User MockUser(string name, PkmnSpecies? selectedBadge = null) => new User(
            id: Guid.NewGuid().ToString(),
            name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0,
            selectedBadge: selectedBadge);

        private static Message MockMessage(User user, string text = "") =>
            new(user, text, MessageSource.Chat, string.Empty);

        private Mock<IUserRepo> _userRepoMock = null!;
        private ArgsParser _argsParser = null!;
        private Mock<IBank<User>> _pokeyenBankMock = null!;
        private Mock<IBank<User>> _tokensBankMock = null!;
        private Mock<IMessageSender> _messageSenderMock = null!;

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
            _argsParser.AddArgumentParser(new UserParser(_userRepoMock.Object));
            _pokeyenBankMock = new Mock<IBank<User>>();
            _tokensBankMock = new Mock<IBank<User>>();
            _messageSenderMock = new Mock<IMessageSender>();
        }

        private static bool AreTransactionsEqual(Transaction<User> tx1, Transaction<User> tx2)
            => tx1.User == tx2.User
               && tx1.Change == tx2.Change
               && tx1.Type == tx2.Type
               && tx1.AdditionalData.DictionaryEqual(tx2.AdditionalData);

        [Test]
        public async Task adjust_tokens_self()
        {
            User user = MockUser("MockUser");
            _userRepoMock.Setup(u => u.FindBySimpleName(user.SimpleName)).ReturnsAsync(user);
            OperatorCommands operatorCommands = new OperatorCommands(
                new StopToken(),
                operatorNames: new[] { user.SimpleName },
                _tokensBankMock.Object, _tokensBankMock.Object,
                _messageSenderMock.Object);

            {
                CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(user),
                    ImmutableList.Create(user.Name, "T123", "because"), _argsParser));
                Assert.AreEqual("Your token balance was adjusted by +123. Reason: because", result.Response);

                var transaction = new Transaction<User>(user, 123, "manual_adjustment",
                    new Dictionary<string, object?> { ["responsible_user"] = user.Id });
                _tokensBankMock.Verify(b => b.PerformTransaction(
                    It.Is<Transaction<User>>(tx => AreTransactionsEqual(tx, transaction)), default), Times.Once);
            }
            {
                CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(user),
                    ImmutableList.Create(user.Name, "T-123", "because"), _argsParser));
                Assert.AreEqual("Your token balance was adjusted by -123. Reason: because", result.Response);

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
            User userSelf = MockUser("MockUserSelf");
            User userOther = MockUser("MockUserOther");
            _userRepoMock.Setup(u => u.FindBySimpleName(userOther.SimpleName)).ReturnsAsync(userOther);
            OperatorCommands operatorCommands = new OperatorCommands(
                new StopToken(),
                operatorNames: new[] { userSelf.SimpleName },
                _tokensBankMock.Object, _tokensBankMock.Object,
                _messageSenderMock.Object);

            {
                CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(userSelf),
                    ImmutableList.Create(userOther.Name, "T123", "because"), _argsParser));
                Assert.AreEqual("MockUserOther's token balance was adjusted by +123. Reason: because", result.Response);

                var transaction = new Transaction<User>(userOther, 123, "manual_adjustment",
                    new Dictionary<string, object?> { ["responsible_user"] = userSelf.Id });
                _tokensBankMock.Verify(b => b.PerformTransaction(
                    It.Is<Transaction<User>>(tx => AreTransactionsEqual(tx, transaction)), default), Times.Once);

                const string notification = "MockUserSelf adjusted your token balance by +123. Reason: because";
                _messageSenderMock.Verify(m => m.SendWhisper(userOther,
                    notification), Times.Once);
            }
            {
                CommandResult result = await operatorCommands.AdjustTokens(new CommandContext(MockMessage(userSelf),
                    ImmutableList.Create(userOther.Name, "T-123", "because"), _argsParser));
                Assert.AreEqual("MockUserOther's token balance was adjusted by -123. Reason: because", result.Response);

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
    }
}
