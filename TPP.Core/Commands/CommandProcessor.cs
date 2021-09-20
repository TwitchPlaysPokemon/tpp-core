using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.ArgsParsing;
using TPP.Core.Utils;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands
{
    public interface ICommandProcessor
    {
        public Task<CommandResult?> Process(string commandName, IImmutableList<string> args, Message message);
        public Command? FindCommand(string commandName);
        public void InstallCommand(Command command);
        public void UninstallCommand(params string[] commandOrAlias);
    }

    /// <summary>
    /// The command processor can be configured using <see cref="Command"/> instances to have commands,
    /// which then get executed using the <see cref="CommandProcessor.Process"/> method.
    /// </summary>
    public class CommandProcessor : ICommandProcessor
    {
        /// <summary>
        /// maximum execution time for a command before a warning is logged.
        /// </summary>
        private static readonly TimeSpan CommandWarnTimeLimit = TimeSpan.FromMilliseconds(1000);

        private readonly ILogger<CommandProcessor> _logger;
        private readonly ICommandLogger _commandLogger;
        private readonly ArgsParser _argsParser;
        private readonly IClock _clock;

        private readonly Dictionary<string, Command> _commands = new();

        private readonly float _maxLoadFactor;
        private readonly Duration _maxLoadFactorTimeframe;
        private readonly float _additionalLoadFactorAtHighThreshold;
        private Dictionary<User, TtlQueue<float>> _loadsPerUser = new();

        /// <summary>
        /// Create a new command processor instance
        /// </summary>
        /// <param name="logger">logger</param>
        /// <param name="commandLogger">command logger</param>
        /// <param name="argsParser">args parser instance</param>
        /// <param name="clock">clock</param>
        /// <param name="maxLoadFactor">maximum load factor before commands are silently dropped</param>
        /// <param name="maxLoadFactorTimeframe">timeframe for which the load factor is computed</param>
        /// <param name="additionalLoadFactorAtHighThreshold">
        /// additional load to add to the load factor when a user is at their maximum load capacity.
        /// It is linearly interpolated from 0 when there are no messages within the timeframe,
        /// up to the supplied number multiplier when at the maximum amount of messages within the timeframe.
        /// This is to have the load factor be more effective against continuous spam than sporadic bursts.</param>
        public CommandProcessor(
            ILogger<CommandProcessor> logger,
            ICommandLogger commandLogger,
            ArgsParser argsParser,
            IClock clock,
            float maxLoadFactor = 200f,
            Duration? maxLoadFactorTimeframe = null,
            float additionalLoadFactorAtHighThreshold = 2f)
        {
            _logger = logger;
            _commandLogger = commandLogger;
            _argsParser = argsParser;
            _clock = clock;
            _maxLoadFactor = maxLoadFactor;
            _maxLoadFactorTimeframe = maxLoadFactorTimeframe ?? Duration.FromMinutes(10);
            _additionalLoadFactorAtHighThreshold = additionalLoadFactorAtHighThreshold;
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

        private float CheckAndUpdateLoadFactorForUser(User user)
        {
            _loadsPerUser = _loadsPerUser
                .Where(kvp => kvp.Value.Count > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            if (!_loadsPerUser.TryGetValue(user, out TtlQueue<float>? loads))
            {
                loads = new TtlQueue<float>(_maxLoadFactorTimeframe, _clock);
                _loadsPerUser[user] = loads;
            }
            float sum = loads.Sum();
            float ratioFilled = Math.Min(1, sum / _maxLoadFactor);
            float toAdd = 1 + ratioFilled * _additionalLoadFactorAtHighThreshold;
            loads.Enqueue(toAdd);
            return sum + toAdd;
        }

        public async Task<CommandResult?> Process(string commandName, IImmutableList<string> args, Message message)
        {
            float loadFactor = CheckAndUpdateLoadFactorForUser(message.User);
            if (loadFactor > _maxLoadFactor)
            {
                _logger.LogDebug(
                    "command '{Command}' from user {User} ignored because load factor is {LoadFactor} " +
                    "for timeframe {Duration}, which is above the maximum of {MaxLoadFactor}",
                    commandName, message.User, loadFactor, _maxLoadFactorTimeframe, _maxLoadFactor);
                return new CommandResult();
            }
            if (!_commands.TryGetValue(commandName.ToLower(), out Command command))
            {
                _logger.LogDebug("unknown command '{Command}'", commandName);
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
                    "An exception occured while executing command '{Command}'. User: {User}, Original text: {MessageText}",
                    command.Name, message.User, message.MessageText);
                result = new CommandResult { Response = "An error occurred." };
            }
            stopwatch.Stop();
            if (stopwatch.Elapsed >= CommandWarnTimeLimit)
            {
                _logger.LogWarning(
                    "Command '{Command}' took unusually long ({Duration}ms) to finish! " +
                    "User: {User:l}, Original text: {MessageText}",
                    command.Name, stopwatch.ElapsedMilliseconds, message.User, message.MessageText);
            }
            return result;
        }
    }
}
