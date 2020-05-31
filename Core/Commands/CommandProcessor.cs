using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArgsParsing;
using Microsoft.Extensions.Logging;

namespace Core.Commands
{
    /// <summary>
    /// The command processor can be configured using <see cref="CommandInfo"/> instances to have commands,
    /// which then get executed using the <see cref="CommandProcessor.Process"/> method.
    /// </summary>
    public class CommandProcessor
    {
        /// <summary>
        /// maximum execution time for a command before a warning is logged.
        /// </summary>
        private static readonly TimeSpan CommandWarnTimeLimit = TimeSpan.FromMilliseconds(50);

        private readonly ILogger<CommandProcessor> _logger;
        private readonly ArgsParser _argsParser;
        private readonly Dictionary<string, CommandInfo> _commands = new Dictionary<string, CommandInfo>();

        public CommandProcessor(ILogger<CommandProcessor> logger, ArgsParser argsParser)
        {
            _logger = logger;
            _argsParser = argsParser;
        }

        public void InstallCommand(CommandInfo commandInfo)
        {
            string command = commandInfo.Command.ToLower();
            if (_commands.ContainsKey(command))
            {
                throw new ArgumentException($"The command name '{command}' conflicts with: {_commands[command]}");
            }
            foreach (string alias in commandInfo.Aliases.Select(a => a.ToLower()))
            {
                if (_commands.ContainsKey(alias))
                {
                    throw new ArgumentException($"The alias '{alias}' conflicts with: {_commands[alias]}");
                }
            }
            _commands[command] = commandInfo;
            foreach (string alias in commandInfo.Aliases.Select(a => a.ToLower()))
            {
                _commands[alias] = commandInfo;
            }
        }

        public void UninstallCommand(CommandInfo commandInfo)
        {
            _commands.Remove(commandInfo.Command.ToLower());
            foreach (string alias in commandInfo.Aliases.Select(a => a.ToLower()))
            {
                _commands.Remove(alias);
            }
        }

        public async Task<CommandResult> Process(string commandName, IImmutableList<string> args, Message message)
        {
            if (!_commands.TryGetValue(commandName.ToLower(), out CommandInfo command))
            {
                return new CommandResult {Response = $"unknown command '{commandName}'"};
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            CommandResult result;
            try
            {
                result = await command.Execution(new CommandContext(message, args, _argsParser));
            }
            catch (ArgsParseFailure ex)
            {
                result = new CommandResult {Response = ex.Message};
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"An exception occured while executing command '{command.Command}'. " +
                    $"User: {message.User}, Original text: {message.MessageText}");
                result = new CommandResult {Response = "An error occurred."};
            }
            stopwatch.Stop();
            if (stopwatch.Elapsed >= CommandWarnTimeLimit)
            {
                _logger.LogWarning(
                    $"Command '{command.Command}' took {stopwatch.ElapsedMilliseconds}ms to finish! " +
                    $"User: {message.User}, Original text: {message.MessageText}");
            }
            return result;
        }
    }
}
