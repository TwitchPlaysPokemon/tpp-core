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
    /// <summary>
    /// This class orchestrates the entirety of the TPP core.
    /// </summary>
    public sealed class Tpp : IDisposable
    {
        private readonly ILogger _logger;
        private readonly RootConfig _rootConfig;
        private readonly CommandProcessor _commandProcessor;
        private readonly IChat _chat;
        private readonly StopToken _stopToken;

        public Tpp(ILoggerFactory loggerFactory, RootConfig rootConfig)
        {
            _logger = loggerFactory.CreateLogger<Tpp>();
            _rootConfig = rootConfig;
            Setups.Databases repos = Setups.SetUpRepositories(rootConfig);
            ArgsParser argsParser = Setups.SetUpArgsParser(repos.UserRepo);

            _stopToken = new StopToken();
            _commandProcessor = Setups.SetUpCommandProcessor(
                loggerFactory, argsParser, _stopToken, rootConfig.Irc.OperatorNames);

            _chat = new TwitchChat(loggerFactory, SystemClock.Instance, rootConfig.Irc, repos.UserRepo);
            _chat.IncomingMessage += MessageReceived;
        }

        private async void MessageReceived(object? sender, MessageEventArgs e) =>
            await ProcessIncomingMessage(e.Message);

        private async Task ProcessIncomingMessage(Message message)
        {
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
                    if (message.MessageSource == MessageSource.Chat)
                        await _chat.SendMessage($"@{message.User.TwitchDisplayName} {result.Response}");
                    else if (message.MessageSource == MessageSource.Whisper)
                        await _chat.SendWhisper(message.User, result.Response);
                    else
                        throw new ArgumentOutOfRangeException(nameof(message));
                }
            }
        }

        public async Task Run()
        {
            _logger.LogInformation("Hi!");
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            using IDisposable chatConnection = _chat.EstablishConnection();
            while (!_stopToken.ShouldStop)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            // TODO do all the things
            _logger.LogInformation("Bye!");
        }

        public void Dispose()
        {
            _chat.IncomingMessage -= MessageReceived;
        }
    }
}
