using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.ArgsParsing;
using TPP.Core.Chat;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Core.Configuration;
using TPP.Persistence.Repos;

namespace TPP.Core.Modes
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
        private readonly ProcessMessage _processMessage;

        /// Processes a message that wasn't already processed by the mode base,
        /// and returns whether the message was actively processed.
        public delegate Task<bool> ProcessMessage(Message message);

        public ModeBase(
            ILoggerFactory loggerFactory,
            BaseConfig baseConfig,
            StopToken stopToken,
            ProcessMessage? processMessage = null)
        {
            PokedexData pokedexData = PokedexData.Load();
            Setups.Databases repos = Setups.SetUpRepositories(baseConfig);
            ArgsParser argsParser = Setups.SetUpArgsParser(repos.UserRepo, pokedexData);
            _processMessage = processMessage ?? (_ => Task.FromResult(false));

            TwitchChat twitchChat = new(loggerFactory, SystemClock.Instance, baseConfig.Chat, repos.UserRepo);
            _chat = twitchChat;
            _chat.IncomingMessage += MessageReceived;
            _chat.IncomingUnhandledIrcLine += UnhandledIrcLineReceived;
            _commandResponder = new CommandResponder(_chat);

            _commandProcessor = Setups.SetUpCommandProcessor(
                loggerFactory, argsParser, repos, stopToken, baseConfig.Chat, twitchChat);

            _messagequeueRepo = repos.MessagequeueRepo;
            _messagelogRepo = repos.MessagelogRepo;
            _forwardUnprocessedMessages = baseConfig.Chat.ForwardUnprocessedMessages;
            _clock = SystemClock.Instance;
        }

        public void InstallAdditionalCommand(Command command)
        {
            _commandProcessor.InstallCommand(command);
        }

        private async void MessageReceived(object? sender, MessageEventArgs e) =>
            await ProcessIncomingMessage(e.Message);

        private async void UnhandledIrcLineReceived(object? sender, string unhandledIrcLine)
        {
            if (_forwardUnprocessedMessages)
                await _messagequeueRepo.EnqueueMessage(unhandledIrcLine);
        }

        private async Task ProcessIncomingMessage(Message message)
        {
            await _messagelogRepo.LogChat(
                message.User.Id, message.RawIrcMessage, message.MessageText, _clock.GetCurrentInstant());

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
            wasProcessed |= await _processMessage(message);
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
            _chat.IncomingUnhandledIrcLine -= UnhandledIrcLineReceived;
        }
    }
}
