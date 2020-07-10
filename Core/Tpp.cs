using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ArgsParsing;
using Core.Commands;
using Core.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Persistence.Models;

namespace Core
{
    /// <summary>
    /// This class orchestrates the entirety of the TPP core.
    /// </summary>
    public class Tpp
    {
        private readonly ILogger _logger;
        private readonly RootConfig _rootConfig;
        private readonly CommandProcessor _commandProcessor;

        public Tpp(ILoggerFactory loggerFactory, RootConfig rootConfig)
        {
            _logger = loggerFactory.CreateLogger<Tpp>();
            _rootConfig = rootConfig;
            Setups.Databases repos = Setups.SetUpRepositories(rootConfig);
            ArgsParser argsParser = Setups.SetUpArgsParser(repos.UserRepo);
            _commandProcessor = Setups.SetUpCommandProcessor(loggerFactory, argsParser);
        }

        private async Task OnMessage(Message message)
        {
            _logger.LogDebug($"< {message.User}: {message.MessageText}");
            string[] parts = message.MessageText.Split(" ");
            string? firstPart = parts.FirstOrDefault();
            string? commandName = firstPart switch
            {
                var name when message.MessageSource == MessageSource.Whisper => name,
                var name when message.MessageSource == MessageSource.Chat
                              && name.StartsWith("!") => name.Substring(startIndex: 1),
                _ => null
            };
            if (commandName != null)
            {
                CommandResult result = await _commandProcessor
                    .Process(commandName, parts.Skip(1).ToImmutableList(), message);
                if (result.Response != null)
                {
                    _logger.LogDebug($"> {message.User}: {result.Response}");
                }
            }
        }

        public async Task Run()
        {
            _logger.LogInformation("Hi!");
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            // not actually hooked up yet, mock an incoming message
            var user = new User("userid", "user", "user", "user", color: null,
                Instant.FromUnixTimeSeconds(0), Instant.FromUnixTimeSeconds(0), null, 0, 0);
            await OnMessage(new Message(user, "!thanks", MessageSource.Chat));
            await OnMessage(new Message(user, "thanks", MessageSource.Whisper));

            // TODO do all the things
            _logger.LogInformation("Bye!");
        }
    }
}
