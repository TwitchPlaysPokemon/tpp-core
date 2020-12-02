using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArgsParsing;
using Core.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using NUnit.Framework;
using Persistence.Models;

namespace Core.Tests.Commands
{
    public class CommandProcessorTest
    {
        private readonly ILogger<CommandProcessor> _nullLogger = new NullLogger<CommandProcessor>();
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
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());

            CommandResult? result = await commandProcessor.Process("unknown", _noArgs, MockMessage());

            Assert.IsNull(result);
        }

        [Test]
        [Category("IntegrationTest")] // test performs a sleep
        public async Task TestLogSlowCommand()
        {
            var loggerMock = new Mock<ILogger<CommandProcessor>>();
            var commandProcessor = new CommandProcessor(loggerMock.Object, new ArgsParser());
            commandProcessor.InstallCommand(new Command("slow", async context =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300));
                return new CommandResult();
            }));

            await commandProcessor.Process("slow", _noArgs, MockMessage("bla"));

            string warningTextRegex = @"^Command 'slow' took \d+ms to finish! " +
                                      $@"User: {Regex.Escape(_mockUser.ToString())}, Original text: bla$";
            loggerMock.VerifyLog(logger => logger.LogWarning(It.IsRegex(warningTextRegex)));
        }

        [Test]
        public async Task TestCommandThrowsError()
        {
            var loggerMock = new Mock<ILogger<CommandProcessor>>();
            var commandProcessor = new CommandProcessor(loggerMock.Object, new ArgsParser());
            commandProcessor.InstallCommand(new Command("broken",
                context => throw new InvalidOperationException("this command is busted!")));

            CommandResult? result = await commandProcessor.Process("broken", _noArgs, MockMessage("bla"));

            Assert.AreEqual("An error occurred.", result?.Response);
            string errorTextRegex = @"^An exception occured while executing command 'broken'\. " +
                                     $@"User: {Regex.Escape(_mockUser.ToString())}, Original text: bla$";
            loggerMock.VerifyLog(logger => logger.LogError(
                new InvalidOperationException("this command is busted!"), It.IsRegex(errorTextRegex)));
        }

        [Test]
        public async Task TestCaseInsensitive()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());
            commandProcessor.InstallCommand(new Command("MiXeD", CommandUtils.StaticResponse("Hi!")));

            foreach (string command in ImmutableList.Create("MiXeD", "mixed", "MIXED"))
            {
                CommandResult? result = await commandProcessor.Process(command, _noArgs, MockMessage());
                Assert.AreEqual("Hi!", result?.Response);
            }
        }

        [Test]
        public async Task TestAliases()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());
            commandProcessor.InstallCommand(new Command("main", CommandUtils.StaticResponse("Hi!"))
            { Aliases = new[] { "alias1", "alias2" } });

            foreach (string command in ImmutableList.Create("main", "alias1", "ALIAS2"))
            {
                CommandResult? result = await commandProcessor.Process(command, _noArgs, MockMessage());
                Assert.AreEqual("Hi!", result?.Response);
            }
        }

        [Test]
        public void InstallConflictName()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());

            commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!")));
            var ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new Command("A", CommandUtils.StaticResponse("Hi!"))));
            Assert.AreEqual("The command name 'a' conflicts with: a: <no description>", ex.Message);
        }

        [Test]
        public void InstallConflictAlias()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());

            commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!"))
            { Aliases = new[] { "x" } });
            var ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new Command("b", CommandUtils.StaticResponse("Hi!")) { Aliases = new[] { "X" } }));
            Assert.AreEqual("The alias 'x' conflicts with: a(x): <no description>", ex.Message);
        }

        [Test]
        public void InstallConflictNameVsAlias()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());

            commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!"))
            { Aliases = new[] { "b" } });
            var ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new Command("b", CommandUtils.StaticResponse("Hi!")) { Aliases = new[] { "x" } }));
            Assert.AreEqual("The command name 'b' conflicts with: a(b): <no description>", ex.Message);
        }
    }
}
