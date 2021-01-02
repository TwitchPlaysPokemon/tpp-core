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
using Persistence.Repos;

namespace Core.Modes
{
    public sealed class ModeBase : IDisposable
    {
        private readonly CommandProcessor _commandProcessor;
        private readonly IChat _chat;
        private readonly ICommandResponder _commandResponder;
        private readonly IMessagequeueRepo _messagequeueRepo;
        private readonly bool _forwardUnprocessedMessages;
        private readonly IMessagelogRepo _messagelogRepo;
        private readonly IClock _clock;

        public ModeBase(ILoggerFactory loggerFactory, BaseConfig baseConfig, StopToken stopToken)
        {
            PokedexData pokedexData = PokedexData.Load();
            Setups.Databases repos = Setups.SetUpRepositories(baseConfig);
            ArgsParser argsParser = Setups.SetUpArgsParser(repos.UserRepo, pokedexData);

            _commandProcessor = Setups.SetUpCommandProcessor(
                loggerFactory, argsParser, repos, stopToken, baseConfig.Chat);

            _chat = new TwitchChat(loggerFactory, SystemClock.Instance, baseConfig.Chat, repos.UserRepo);
            _chat.IncomingMessage += MessageReceived;
            _commandResponder = new CommandResponder(_chat);

            _messagequeueRepo = repos.MessagequeueRepo;
            _messagelogRepo = repos.MessagelogRepo;
            _forwardUnprocessedMessages = baseConfig.Chat.ForwardUnprocessedMessages;
            _clock = SystemClock.Instance;
        }

        private async void MessageReceived(object? sender, MessageEventArgs e) =>
            await ProcessIncomingMessage(e.Message);

        private async Task ProcessIncomingMessage(Message message)
        {
            await _messagelogRepo.LogChat(
                message.User, message.RawIrcMessage, message.MessageText, _clock.GetCurrentInstant());

            string[] parts = message.MessageText.Split(" ");
            string? firstPart = parts.FirstOrDefault();
            string? commandName = firstPart switch
            {
                null => null,
                var name when message.MessageSource == MessageSource.Whisper
                    => name.StartsWith('!') ? name.Substring(startIndex: 1) : name,
                var name when message.MessageSource == MessageSource.Chat && name.StartsWith('!')
                    => name.Substring(startIndex: 1),
                _ => null
            };
            bool wasProcessed = false;
            if (commandName != null)
            {
                CommandResult? result = await _commandProcessor
                    .Process(commandName, parts.Skip(1).ToImmutableList(), message);
                if (result != null)
                {
                    await _commandResponder.ProcessResponse(message, result);
                    wasProcessed = true;
                }
                else if (!_forwardUnprocessedMessages)
                {
                    await _commandResponder.ProcessResponse(message, new CommandResult
                    {
                        Response = $"unknown command '{commandName}'",
                        ResponseTarget = ResponseTarget.Whisper
                    });
                    wasProcessed = true;
                }
            }
            if (!wasProcessed && _forwardUnprocessedMessages)
            {
                await _messagequeueRepo.EnqueueMessage(message.RawIrcMessage);
            }
        }

        public void Start()
        {
            _chat.Connect();
        }

        public void Dispose()
        {
            _chat.Dispose();
            _chat.IncomingMessage -= MessageReceived;
        }
    }
}
