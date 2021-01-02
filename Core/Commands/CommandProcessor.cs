using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArgsParsing;
using Microsoft.Extensions.Logging;
using Persistence.Repos;

namespace Core.Commands
{
    /// <summary>
    /// The command processor can be configured using <see cref="Command"/> instances to have commands,
    /// which then get executed using the <see cref="CommandProcessor.Process"/> method.
    /// </summary>
    public class CommandProcessor
    {
        /// <summary>
        /// maximum execution time for a command before a warning is logged.
        /// </summary>
        private static readonly TimeSpan CommandWarnTimeLimit = TimeSpan.FromMilliseconds(50);

        private readonly ILogger<CommandProcessor> _logger;
        private readonly ICommandLogger _commandLogger;
        private readonly ArgsParser _argsParser;
        private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();

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

        public void UninstallCommand(Command command)
        {
            _commands.Remove(command.Name.ToLower());
            foreach (string alias in command.Aliases.Select(a => a.ToLower()))
            {
                _commands.Remove(alias);
            }
        }

        public Command? FindCommand(string commandName) =>
            _commands.TryGetValue(commandName.ToLower(), out Command command) ? command : null;

        public async Task<CommandResult?> Process(string commandName, IImmutableList<string> args, Message message)
        {
            if (!_commands.TryGetValue(commandName.ToLower(), out Command command))
            {
                _logger.LogDebug($"unknown command '{commandName}'");
                return null;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();
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
                    $"An exception occured while executing command '{command.Name}'. " +
                    $"User: {message.User}, Original text: {message.MessageText}");
                result = new CommandResult { Response = "An error occurred." };
            }
            stopwatch.Stop();
            if (stopwatch.Elapsed >= CommandWarnTimeLimit)
            {
                _logger.LogWarning(
                    $"Command '{command.Name}' took {stopwatch.ElapsedMilliseconds}ms to finish! " +
                    $"User: {message.User}, Original text: {message.MessageText}");
            }
            return result;
        }
    }
}
