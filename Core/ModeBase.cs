using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ArgsParsing;
using Core.Chat;
using Core.Commands;
using Core.Commands.Definitions;
using Core.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Core
{
    public abstract class ModeBase : IDisposable
    {
        private readonly ILogger _logger;
        private readonly BaseConfig _baseConfig;
        private readonly CommandProcessor _commandProcessor;
        private readonly IChat _chat;
        private readonly ICommandResponder _commandResponder;
        private readonly StopToken _stopToken;

        protected ModeBase(ILoggerFactory loggerFactory, BaseConfig baseConfig)
        {
            _logger = loggerFactory.CreateLogger<ModeBase>();
            _baseConfig = baseConfig;
            PokedexData pokedexData = PokedexData.Load();
            Setups.Databases repos = Setups.SetUpRepositories(baseConfig);
            ArgsParser argsParser = Setups.SetUpArgsParser(repos.UserRepo, pokedexData);

            _stopToken = new StopToken();
            _commandProcessor = Setups.SetUpCommandProcessor(
                loggerFactory, argsParser, repos, _stopToken, baseConfig.Chat);

            _chat = new TwitchChat(loggerFactory, SystemClock.Instance, baseConfig.Chat, repos.UserRepo);
            _chat.IncomingMessage += MessageReceived;
            _commandResponder = new CommandResponder(_chat);
        }

        private async void MessageReceived(object? sender, MessageEventArgs e) =>
            await ProcessIncomingMessage(e.Message);

        private async Task ProcessIncomingMessage(Message message)
        {
            string[] parts = message.MessageText.Split(" ");
            string? firstPart = parts.FirstOrDefault();
            string? commandName = firstPart switch
            {
                null => null,
                var name when message.MessageSource == MessageSource.Whisper => name,
                var name when message.MessageSource == MessageSource.Chat
                              && name.StartsWith("!") => name.Substring(startIndex: 1),
                _ => null
            };
            if (commandName != null)
            {
                CommandResult result = await _commandProcessor
                    .Process(commandName, parts.Skip(1).ToImmutableList(), message);
                await _commandResponder.ProcessResponse(message, result);
            }
        }

        public async Task Run()
        {
            _logger.LogInformation("Hi!");
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            _chat.Connect();
            while (!_stopToken.ShouldStop)
            {
                // TODO main loop goes here
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            _logger.LogInformation("Bye!");
        }

        public void Dispose()
        {
            _chat.Dispose();
            _chat.IncomingMessage -= MessageReceived;
        }
    }
}
