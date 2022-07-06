using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.ArgsParsing;
using TPP.Persistence;

namespace TPP.Core.Commands
{
    /// <summary>
    /// The command processor can be configured using <see cref="Command"/> instances to have commands,
    /// which then get executed using the <see cref="CommandProcessor.Process"/> method.
    /// </summary>
    public class CommandProcessor
    {
        private readonly ILogger<CommandProcessor> _logger;
        private readonly ICommandLogger _commandLogger;
        private readonly ArgsParser _argsParser;
        private readonly Dictionary<string, Command> _commands = new();

        public CommandProcessor(
            ILogger<CommandProcessor> logger,
            ICommandLogger commandLogger,
            ArgsParser argsParser)
        {
            _logger = logger;
            _commandLogger = commandLogger;
            _argsParser = argsParser;
        }

        public void InstallCommand(Command command)
        {
            string commandName = command.Name.ToLower();
            if (_commands.ContainsKey(commandName))
            {
                throw new ArgumentException(
                    $"The command name '{commandName}' conflicts with: {_commands[commandName]}");
            }
            foreach (string alias in command.Aliases.Select(a => a.ToLower()))
            {
                if (_commands.ContainsKey(alias))
                {
                    throw new ArgumentException($"The alias '{alias}' conflicts with: {_commands[alias]}");
                }
            }
            _commands[commandName] = command;
            foreach (string alias in command.Aliases.Select(a => a.ToLower()))
            {
                _commands[alias] = command;
            }
        }

        public void UninstallCommand(params string[] commandOrAlias)
        {
            foreach (string name in commandOrAlias.Select(a => a.ToLower()))
            {
                _commands.Remove(name);
            }
        }

        public Command? FindCommand(string commandName) =>
            _commands.TryGetValue(commandName.ToLower(), out Command command) ? command : null;

        public async Task<CommandResult?> Process(string commandName, IImmutableList<string> args, Message message)
        {
            if (!_commands.TryGetValue(commandName.ToLower(), out Command command))
            {
                _logger.LogDebug("unknown command '{Command}'", commandName);
                return null;
            }
            CommandResult result;
            try
            {
                result = await command.Execution(new CommandContext(message, args, _argsParser));
                await _commandLogger.Log(message.User.Id, commandName, args, result.Response);
            }
            catch (ArgsParseFailure ex)
            {
                result = new CommandResult { Response = ex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "An exception occured while executing command '{Command}'. User: {User}, Original text: {MessageText}",
                    command.Name, message.User, message.MessageText);
                result = new CommandResult { Response = "An error occurred." };
            }
            return result;
        }
    }
}
