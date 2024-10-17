using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.ArgsParsing;
using TPP.Core.Chat;
using TPP.Persistence;

namespace TPP.Core.Commands;

/// <summary>
/// The command processor can be configured using <see cref="Command"/> instances to have commands,
/// which then get executed using the <see cref="CommandProcessor.Process"/> method.
/// </summary>
public class CommandProcessor(
    ILogger<CommandProcessor> logger,
    ICommandLogger commandLogger,
    ArgsParser argsParser)
{
    private readonly Dictionary<string, Command> _commands = new();

    public void InstallCommand(Command command)
    {
        string commandName = command.Name.ToLower();
        if (_commands.TryGetValue(commandName, out Command existingCommand))
            throw new ArgumentException($"The command name '{commandName}' conflicts with: {existingCommand}");
        foreach (string alias in command.Aliases.Select(a => a.ToLower()))
            if (_commands.TryGetValue(alias, out Command existingCommandForAlias))
                throw new ArgumentException($"The alias '{alias}' conflicts with: {existingCommandForAlias}");

        _commands[commandName] = command;
        foreach (string alias in command.Aliases.Select(a => a.ToLower()))
            _commands[alias] = command;
    }

    public void UninstallCommand(params string[] commandOrAlias)
    {
        foreach (string name in commandOrAlias.Select(a => a.ToLower()))
            _commands.Remove(name);
    }

    public Command? FindCommand(string commandName) =>
        _commands.TryGetValue(commandName.ToLower(), out Command command) ? command : null;

    public async Task<CommandResult?> Process(
        string commandName, IImmutableList<string> args, Message message, IChat? source = null)
    {
        if (!_commands.TryGetValue(commandName.ToLower(), out Command command))
        {
            logger.LogDebug("unknown command '{Command}'", commandName);
            return null;
        }
        try
        {
            CommandResult result = await command.Execution(new CommandContext(message, args, argsParser, source));
            await commandLogger.Log(message.User.Id, commandName, args, result.Response);
            return result;
        }
        catch (ArgsParseFailure ex)
        {
            return new CommandResult { Response = ex.Message };
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "An exception occured while executing command '{Command}'. User: {User}, Original text: {MessageText}",
                command.Name, message.User, message.MessageText);
            return new CommandResult { Response = "An error occurred." };
        }
    }
}
