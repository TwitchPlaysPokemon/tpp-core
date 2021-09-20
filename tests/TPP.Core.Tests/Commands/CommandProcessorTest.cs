using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.ArgsParsing;
using TPP.Core.Commands;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests.Commands
{
    public class CommandProcessorTest
    {
        private readonly ILogger<CommandProcessor> _nullLogger = new NullLogger<CommandProcessor>();
        private readonly Mock<ICommandLogger> _commandLoggerMock = new();
        private readonly ImmutableList<string> _noArgs = ImmutableList<string>.Empty;
        private static User MockUser() => new(
            id: Guid.NewGuid().ToString(),
            name: "MockUser", twitchDisplayName: "☺MockUser", simpleName: "mockuser", color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0);
        private readonly User _mockUser = MockUser();

        private Message MockMessage(string text = "") => new(_mockUser, text, MessageSource.Chat, string.Empty);

        [Test]
        public async Task TestUnknownCommand()
        {
            var commandProcessor = new CommandProcessor(
                _nullLogger, _commandLoggerMock.Object, new ArgsParser(), Mock.Of<IClock>());

            CommandResult? result = await commandProcessor.Process("unknown", _noArgs, MockMessage());

            Assert.IsNull(result);
        }

        [Test]
        [Category("IntegrationTest")] // test performs a sleep
        public async Task TestLogSlowCommand()
        {
            var loggerMock = new Mock<ILogger<CommandProcessor>>();
            var commandProcessor = new CommandProcessor(
                loggerMock.Object, _commandLoggerMock.Object, new ArgsParser(), Mock.Of<IClock>());
            commandProcessor.InstallCommand(new Command("slow", async _ =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1050));
                return new CommandResult();
            }));

            await commandProcessor.Process("slow", _noArgs, MockMessage("bla"));

            string warningTextRegex = @"^Command 'slow' took unusually long \(\d+ms\) to finish! " +
                                      $@"User: {Regex.Escape(_mockUser.ToString())}, Original text: bla$";
            loggerMock.VerifyLog(logger => logger.LogWarning(It.IsRegex(warningTextRegex)));
        }

        [Test]
        public async Task TestCommandThrowsError()
        {
            var loggerMock = new Mock<ILogger<CommandProcessor>>();
            var commandProcessor = new CommandProcessor(
                loggerMock.Object, _commandLoggerMock.Object, new ArgsParser(), Mock.Of<IClock>());
            commandProcessor.InstallCommand(new Command("broken",
                _ => throw new InvalidOperationException("this command is busted!")));

            CommandResult? result = await commandProcessor.Process("broken", _noArgs, MockMessage("bla"));

            Assert.That(result?.Response, Is.EqualTo("An error occurred."));
            string errorTextRegex = @"^An exception occured while executing command 'broken'\. " +
                                     $@"User: {Regex.Escape(_mockUser.ToString())}, Original text: bla$";
            loggerMock.VerifyLog(logger => logger.LogError(
                new InvalidOperationException("this command is busted!"), It.IsRegex(errorTextRegex)));
        }

        [Test]
        public async Task TestCaseInsensitive()
        {
            var commandProcessor = new CommandProcessor(
                _nullLogger, _commandLoggerMock.Object, new ArgsParser(), Mock.Of<IClock>());
            commandProcessor.InstallCommand(new Command("MiXeD", CommandUtils.StaticResponse("Hi!")));

            foreach (string command in ImmutableList.Create("MiXeD", "mixed", "MIXED"))
            {
                CommandResult? result = await commandProcessor.Process(command, _noArgs, MockMessage());
                Assert.That(result?.Response, Is.EqualTo("Hi!"));
            }
        }

        [Test]
        public async Task TestAliases()
        {
            var commandProcessor = new CommandProcessor(
                _nullLogger, _commandLoggerMock.Object, new ArgsParser(), Mock.Of<IClock>());
            commandProcessor.InstallCommand(new Command("main", CommandUtils.StaticResponse("Hi!"))
            { Aliases = new[] { "alias1", "alias2" } });

            foreach (string command in ImmutableList.Create("main", "alias1", "ALIAS2"))
            {
                CommandResult? result = await commandProcessor.Process(command, _noArgs, MockMessage());
                Assert.That(result?.Response, Is.EqualTo("Hi!"));
            }
        }

        [Test]
        public void InstallConflictName()
        {
            var commandProcessor = new CommandProcessor(
                _nullLogger, _commandLoggerMock.Object, new ArgsParser(), Mock.Of<IClock>());

            commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!")));
            ArgumentException ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new Command("A", CommandUtils.StaticResponse("Hi!"))))!;
            Assert.That(ex.Message, Is.EqualTo("The command name 'a' conflicts with: a: <no description>"));
        }

        [Test]
        public void InstallConflictAlias()
        {
            var commandProcessor = new CommandProcessor(
                _nullLogger, _commandLoggerMock.Object, new ArgsParser(), Mock.Of<IClock>());

            commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!"))
            { Aliases = new[] { "x" } });
            ArgumentException ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new Command("b", CommandUtils.StaticResponse("Hi!")) { Aliases = new[] { "X" } }))!;
            Assert.That(ex.Message, Is.EqualTo("The alias 'x' conflicts with: a(x): <no description>"));
        }

        [Test]
        public void InstallConflictNameVsAlias()
        {
            var commandProcessor = new CommandProcessor(
                _nullLogger, _commandLoggerMock.Object, new ArgsParser(), Mock.Of<IClock>());

            commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!"))
            { Aliases = new[] { "b" } });
            ArgumentException ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new Command("b", CommandUtils.StaticResponse("Hi!")) { Aliases = new[] { "x" } }))!;
            Assert.That(ex.Message, Is.EqualTo("The command name 'b' conflicts with: a(b): <no description>"));
        }

        [Test]
        public async Task TestPermissions()
        {
            var commandProcessor = new CommandProcessor(
                _nullLogger, _commandLoggerMock.Object, new ArgsParser(), Mock.Of<IClock>());
            commandProcessor.InstallCommand(new Command("opsonly", CommandUtils.StaticResponse("you are an operator")).WithCondition(
            canExecute: ctx => IsOperator(ctx.Message.User),
            ersatzResult: new CommandResult { Response = "Only operators can use that command" }));
            bool IsOperator(User user) =>
                user.Roles.Contains(Role.Operator);
            User op = new User(
            id: Guid.NewGuid().ToString(),
            name: "operator", twitchDisplayName: "operator", simpleName: "mockoperator", color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0, roles: new HashSet<Role> { Role.Operator });

            CommandResult? userResult = await commandProcessor.Process("opsonly", _noArgs, new Message(_mockUser, "", MessageSource.Chat, ""));
            Assert.That(userResult?.Response, Is.EqualTo("Only operators can use that command"));

            CommandResult? opResult = await commandProcessor.Process("opsonly", _noArgs, new Message(op, "", MessageSource.Chat, ""));
            Assert.That(opResult?.Response, Is.EqualTo("you are an operator"));
        }

        [Test]
        public async Task MaxCommandsPerUser()
        {
            Mock<IClock> clockMock = new();
            var commandProcessor = new CommandProcessor(
                _nullLogger, _commandLoggerMock.Object, new ArgsParser(), clockMock.Object,
                maxLoadFactor: 6, maxLoadFactorTimeframe: Duration.FromSeconds(10),
                additionalLoadFactorAtHighThreshold: 6);

            commandProcessor.InstallCommand(new Command("foo",
                _ => Task.FromResult(new CommandResult {Response = "yes!"})));

            clockMock.Setup(clock => clock.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0));
            CommandResult? resultOk1 = await commandProcessor.Process(
                "foo", ImmutableList.Create(""), new Message(_mockUser, "", MessageSource.Chat, ""));

            // has +1 additional load factor because the load factor is already at 1/6, which * 6 additional load is 1
            // result is a total load of 3
            clockMock.Setup(clock => clock.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(5));
            CommandResult? resultOk2 = await commandProcessor.Process(
                "foo", ImmutableList.Create(""), new Message(_mockUser, "", MessageSource.Chat, ""));

            // at 50% load already. this gets rejected and adds an additional +3 load (50% of additional 6 load)
            // result is a total load of 7
            clockMock.Setup(clock => clock.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(10));
            CommandResult? resultNo = await commandProcessor.Process(
                "foo", ImmutableList.Create(""), new Message(_mockUser, "", MessageSource.Chat, ""));

            // make sure this is per-user
            CommandResult? resultOkOtherUser = await commandProcessor.Process(
                "foo", ImmutableList.Create(""), new Message(MockUser(), "", MessageSource.Chat, ""));

            // letting everything so far expire lets the user use commands again
            clockMock.Setup(clock => clock.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(21));
            CommandResult? resultOk3 = await commandProcessor.Process(
                "foo", ImmutableList.Create(""), new Message(_mockUser, "", MessageSource.Chat, ""));

            Assert.That(resultOk1?.Response, Is.EqualTo("yes!"));
            Assert.That(resultOk2?.Response, Is.EqualTo("yes!"));
            Assert.That(resultNo?.Response, Is.Null);
            Assert.That(resultOkOtherUser?.Response, Is.EqualTo("yes!"));
            Assert.That(resultOk3?.Response, Is.EqualTo("yes!"));
        }
    }
}
