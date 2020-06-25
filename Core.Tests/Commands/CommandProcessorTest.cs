using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArgsParsing;
using Core.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Persistence.Models;
using static Core.Tests.TestUtils;

namespace Core.Tests.Commands
{
    public class CommandProcessorTest
    {
        private readonly ILogger<CommandProcessor> _nullLogger = new NullLogger<CommandProcessor>();
        private readonly ImmutableList<string> _noArgs = ImmutableList<string>.Empty;
        private readonly User _mockUser = MockUser("MockUser");

        private Message MockMessage(string text = "") => new Message(_mockUser, text);

        [Test]
        public async Task TestUnknownCommand()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());

            CommandResult result = await commandProcessor.Process("unknown", _noArgs, MockMessage());

            Assert.AreEqual("unknown command 'unknown'", result.Response);
        }

        [Test]
        [Category("IntegrationTest")] // test performs a sleep
        public async Task TestLogSlowCommand()
        {
            var loggerMock = new Mock<ILogger<CommandProcessor>>();
            var commandProcessor = new CommandProcessor(loggerMock.Object, new ArgsParser());
            commandProcessor.InstallCommand(new CommandInfo("slow", async context =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50));
                return new CommandResult();
            }));

            await commandProcessor.Process("slow", _noArgs, MockMessage("bla"));

            var warningTextRegex = new Regex(
                $@"^Command 'slow' took \d+ms to finish! User: {Regex.Escape(_mockUser.ToString())}, Original text: bla$");
            loggerMock.VerifyLog(LogLevel.Warning, warningTextRegex, Times.Once());
        }

        [Test]
        public async Task TestCommandThrowsError()
        {
            var loggerMock = new Mock<ILogger<CommandProcessor>>();
            var commandProcessor = new CommandProcessor(loggerMock.Object, new ArgsParser());
            commandProcessor.InstallCommand(new CommandInfo("broken",
                context => throw new InvalidOperationException("this command is busted!")));

            CommandResult result = await commandProcessor.Process("broken", _noArgs, MockMessage("bla"));

            Assert.AreEqual("An error occurred.", result.Response);
            var errorTextRegex = new Regex(
                $@"^An exception occured while executing command 'broken'\. User: {Regex.Escape(_mockUser.ToString())}, Original text: bla$");
            loggerMock.VerifyLog(LogLevel.Error, errorTextRegex, Times.Once(),
                exception: new InvalidOperationException("this command is busted!"));
        }

        [Test]
        public async Task TestCaseInsensitive()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());
            commandProcessor.InstallCommand(new CommandInfo("MiXeD", CommandUtils.StaticResponse("Hi!")));

            foreach (string command in ImmutableList.Create("MiXeD", "mixed", "MIXED"))
            {
                CommandResult result = await commandProcessor.Process(command, _noArgs, MockMessage());
                Assert.AreEqual("Hi!", result.Response);
            }
        }

        [Test]
        public async Task TestAliases()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());
            commandProcessor.InstallCommand(new CommandInfo("main", CommandUtils.StaticResponse("Hi!"))
                {Aliases = new[] {"alias1", "alias2"}});

            foreach (string command in ImmutableList.Create("main", "alias1", "ALIAS2"))
            {
                CommandResult result = await commandProcessor.Process(command, _noArgs, MockMessage());
                Assert.AreEqual("Hi!", result.Response);
            }
        }

        [Test]
        public void InstallConflictName()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());

            commandProcessor.InstallCommand(new CommandInfo("a", CommandUtils.StaticResponse("Hi!")));
            var ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new CommandInfo("A", CommandUtils.StaticResponse("Hi!"))));
            Assert.AreEqual("The command name 'a' conflicts with: a: <no description>", ex.Message);
        }

        [Test]
        public void InstallConflictAlias()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());

            commandProcessor.InstallCommand(new CommandInfo("a", CommandUtils.StaticResponse("Hi!"))
                {Aliases = new[] {"x"}});
            var ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new CommandInfo("b", CommandUtils.StaticResponse("Hi!")) {Aliases = new[] {"X"}}));
            Assert.AreEqual("The alias 'x' conflicts with: a(x): <no description>", ex.Message);
        }

        [Test]
        public void InstallConflictNameVsAlias()
        {
            var commandProcessor = new CommandProcessor(_nullLogger, new ArgsParser());

            commandProcessor.InstallCommand(new CommandInfo("a", CommandUtils.StaticResponse("Hi!"))
                {Aliases = new[] {"b"}});
            var ex = Assert.Throws<ArgumentException>(() => commandProcessor
                .InstallCommand(new CommandInfo("b", CommandUtils.StaticResponse("Hi!")) {Aliases = new[] {"x"}}));
            Assert.AreEqual("The command name 'b' conflicts with: a(b): <no description>", ex.Message);
        }
    }
}
