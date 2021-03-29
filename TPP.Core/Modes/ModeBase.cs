using System;
using System.Collections.Generic;
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
        private readonly IImmutableDictionary<string, IChat> _chats;
        private readonly IImmutableDictionary<string, ICommandResponder> _commandResponders;
        private readonly IImmutableDictionary<string, CommandProcessor> _commandProcessors;
        private readonly IMessagequeueRepo _messagequeueRepo;
        private readonly bool _forwardUnprocessedMessages;
        private readonly IMessagelogRepo _messagelogRepo;
        private readonly IClock _clock;

        public ModeBase(
            ILoggerFactory loggerFactory, Setups.Databases repos, BaseConfig baseConfig, StopToken stopToken)
        {
            PokedexData pokedexData = PokedexData.Load();
            ArgsParser argsParser = Setups.SetUpArgsParser(repos.UserRepo, pokedexData);

            var chats = new Dictionary<string, IChat>();
            var chatFactory = new ChatFactory(loggerFactory, SystemClock.Instance, repos.UserRepo);
            foreach (ConnectionConfig connectorConfig in baseConfig.Chat.Connections)
            {
                IChat chat = chatFactory.Create(connectorConfig);
                if (chats.ContainsKey(chat.Name))
                    throw new ArgumentException($"chat name '{chat.Name}' was used multiple times. It must be unique.");
                chats[chat.Name] = chat;
            }
            _chats = chats.ToImmutableDictionary();
            foreach (IChat chat in _chats.Values)
                chat.IncomingMessage += MessageReceived;
            _commandResponders = _chats.Values.ToImmutableDictionary(
                c => c.Name,
                c => (ICommandResponder)new CommandResponder(c));
            _commandProcessors = _chats.Values.ToImmutableDictionary(
                c => c.Name,
                c => Setups.SetUpCommandProcessor(loggerFactory, argsParser, repos, stopToken, baseConfig.Chat, c, c, pokedexData.KnownSpecies));

            _messagequeueRepo = repos.MessagequeueRepo;
            _messagelogRepo = repos.MessagelogRepo;
            _forwardUnprocessedMessages = baseConfig.Chat.ForwardUnprocessedMessages;
            _clock = SystemClock.Instance;
        }

        public void InstallAdditionalCommand(Command command)
        {
            foreach (CommandProcessor commandProcessor in _commandProcessors.Values)
                commandProcessor.InstallCommand(command);
        }

        private async void MessageReceived(object? sender, MessageEventArgs e) =>
            await ProcessIncomingMessage((IChat)sender!, e.Message);

        private async Task ProcessIncomingMessage(IChat chat, Message message)
        {
            await _messagelogRepo.LogChat(
                message.User.Id, message.RawIrcMessage, message.MessageText, _clock.GetCurrentInstant());

            List<string> parts = message.MessageText.Split(" ")
                .Where(s => !string.IsNullOrEmpty(s)).ToList();
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
                CommandResult? result = await _commandProcessors[chat.Name]
                    .Process(commandName, parts.Skip(1).ToImmutableList(), message);
                if (result != null)
                {
                    await _commandResponders[chat.Name].ProcessResponse(message, result);
                    wasProcessed = true;
                }
                else if (!_forwardUnprocessedMessages)
                {
                    await _commandResponders[chat.Name].ProcessResponse(message, new CommandResult
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
            foreach (IChat chat in _chats.Values)
                chat.Connect();
        }

        public void Dispose()
        {
            foreach (IChat chat in _chats.Values)
            {
                chat.Dispose();
                chat.IncomingMessage -= MessageReceived;
            }
        }
    }
}
