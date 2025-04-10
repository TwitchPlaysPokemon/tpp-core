using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using TPP.ArgsParsing;
using TPP.Core.Commands;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests.Commands;

public class CommandProcessorTest
{
    private readonly ILogger<CommandProcessor> _nullLogger = new NullLogger<CommandProcessor>();
    private readonly ICommandLogger _commandLoggerMock = Substitute.For<ICommandLogger>();
    private readonly ImmutableList<string> _noArgs = ImmutableList<string>.Empty;
    private readonly User _mockUser = new(
        id: Guid.NewGuid().ToString(),
        name: "MockUser", twitchDisplayName: "☺MockUser", simpleName: "mockuser", color: null,
        firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
        lastMessageAt: null, pokeyen: 0, tokens: 0);

    private Message MockMessage(string text = "")
        => new(_mockUser, text, new MessageSource.PrimaryChat(), string.Empty);

    [Test]
    public async Task TestUnknownCommand()
    {
        var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock, new ArgsParser());

        CommandResult? result = await commandProcessor.Process("unknown", _noArgs, MockMessage());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task TestCommandThrowsError()
    {
        var loggerMock = Substitute.For<VerifiableMockLogger<CommandProcessor>>();
        var commandProcessor = new CommandProcessor(loggerMock, _commandLoggerMock, new ArgsParser());
        commandProcessor.InstallCommand(new Command("broken",
            _ => throw new InvalidOperationException("this command is busted!")));

        CommandResult? result = await commandProcessor.Process("broken", _noArgs, MockMessage("bla"));

        Assert.That(result?.Response, Is.EqualTo("An error occurred."));
        var errorTextRegex = new Regex(@"^An exception occured while executing command 'broken'\. " +
                                       $"User: {Regex.Escape(_mockUser.ToString())}, Original text: bla$");
        loggerMock.Received(1).Log(LogLevel.Error, Arg.Is<string>(str => errorTextRegex.IsMatch(str)));
    }

    [Test]
    public async Task TestCaseInsensitive()
    {
        var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock, new ArgsParser());
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
        var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock, new ArgsParser());
        commandProcessor.InstallCommand(new Command("main", CommandUtils.StaticResponse("Hi!"))
            { Aliases = ["alias1", "alias2"] });

        foreach (string command in ImmutableList.Create("main", "alias1", "ALIAS2"))
        {
            CommandResult? result = await commandProcessor.Process(command, _noArgs, MockMessage());
            Assert.That(result?.Response, Is.EqualTo("Hi!"));
        }
    }

    [Test]
    public void InstallConflictName()
    {
        var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock, new ArgsParser());

        commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!")));
        ArgumentException ex = Assert.Throws<ArgumentException>(() => commandProcessor
            .InstallCommand(new Command("A", CommandUtils.StaticResponse("Hi!"))))!;
        Assert.That(ex.Message, Is.EqualTo("The command name 'a' conflicts with: a: <no description>"));
    }

    [Test]
    public void InstallConflictAlias()
    {
        var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock, new ArgsParser());

        commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!"))
            { Aliases = ["x"] });
        ArgumentException ex = Assert.Throws<ArgumentException>(() => commandProcessor
            .InstallCommand(new Command("b", CommandUtils.StaticResponse("Hi!")) { Aliases = ["X"] }))!;
        Assert.That(ex.Message, Is.EqualTo("The alias 'x' conflicts with: a(x): <no description>"));
    }

    [Test]
    public void InstallConflictNameVsAlias()
    {
        var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock, new ArgsParser());

        commandProcessor.InstallCommand(new Command("a", CommandUtils.StaticResponse("Hi!"))
            { Aliases = ["b"] });
        ArgumentException ex = Assert.Throws<ArgumentException>(() => commandProcessor
            .InstallCommand(new Command("b", CommandUtils.StaticResponse("Hi!")) { Aliases = ["x"] }))!;
        Assert.That(ex.Message, Is.EqualTo("The command name 'b' conflicts with: a(b): <no description>"));
    }

    [Test]
    public async Task TestPermissions()
    {
        var commandProcessor = new CommandProcessor(_nullLogger, _commandLoggerMock, new ArgsParser());
        commandProcessor.InstallCommand(new Command("opsonly", CommandUtils.StaticResponse("you are an operator")).WithCondition(
            canExecute: ctx => IsOperator(ctx.Message.User),
            ersatzResult: new CommandResult { Response = "Only operators can use that command" }));
        bool IsOperator(User user) =>
            user.Roles.Contains(Role.Operator);
        User op = new User(
            id: Guid.NewGuid().ToString(),
            name: "operator", twitchDisplayName: "operator", simpleName: "mockoperator", color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0, roles: [Role.Operator]);

        CommandResult? userResult = await commandProcessor.Process("opsonly", _noArgs, new Message(_mockUser, "", new MessageSource.PrimaryChat(), ""));
        Assert.That(userResult?.Response, Is.EqualTo("Only operators can use that command"));

        CommandResult? opResult = await commandProcessor.Process("opsonly", _noArgs, new Message(op, "", new MessageSource.PrimaryChat(), ""));
        Assert.That(opResult?.Response, Is.EqualTo("you are an operator"));
    }
}
