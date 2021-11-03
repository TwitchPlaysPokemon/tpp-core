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
        private readonly User _mockUser = new User(
            id: Guid.NewGuid().ToString(),
            name: "MockUser", twitchDisplayName: "☺MockUser", simpleName: "mockuser", color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0);

        private Message MockMessage(string text = "")
            => new Message(_mockUser, text, MessageSource.Chat, string.Empty);

        [Test]
        public async Task TestUnknownCommand()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock.Object, new ArgsParser());

            CommandResult? result = await commandProcessor.Process("unknown", _noArgs, MockMessage());

            Assert.IsNull(result);
        }

        [Test]
        public async Task TestCommandThrowsError()
        {
            var loggerMock = new Mock<ILogger<CommandProcessor>>();
            var commandProcessor = new CommandProcessor(loggerMock.Object, _commandLoggerMock.Object, new ArgsParser());
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
            var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock.Object, new ArgsParser());
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
            var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock.Object, new ArgsParser());
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
            var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock.Object, new ArgsParser());

            commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!")));
            ArgumentException ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new Command("A", CommandUtils.StaticResponse("Hi!"))))!;
            Assert.That(ex.Message, Is.EqualTo("The command name 'a' conflicts with: a: <no description>"));
        }

        [Test]
        public void InstallConflictAlias()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock.Object, new ArgsParser());

            commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!"))
            { Aliases = new[] { "x" } });
            ArgumentException ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new Command("b", CommandUtils.StaticResponse("Hi!")) { Aliases = new[] { "X" } }))!;
            Assert.That(ex.Message, Is.EqualTo("The alias 'x' conflicts with: a(x): <no description>"));
        }

        [Test]
        public void InstallConflictNameVsAlias()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock.Object, new ArgsParser());

            commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!"))
            { Aliases = new[] { "b" } });
            ArgumentException ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new Command("b", CommandUtils.StaticResponse("Hi!")) { Aliases = new[] { "x" } }))!;
            Assert.That(ex.Message, Is.EqualTo("The command name 'b' conflicts with: a(b): <no description>"));
        }

        [Test]
        public async Task TestPermissions()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock.Object, new ArgsParser());
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
    }
}
