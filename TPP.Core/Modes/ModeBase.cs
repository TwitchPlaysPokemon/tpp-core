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
using TPP.Core.Moderation;
using TPP.Core.Overlay;
using TPP.Inputting;
using TPP.Model;
using TPP.Persistence;
using static TPP.Core.EventUtils;

namespace TPP.Core.Modes
{
    public sealed class ModeBase : IDisposable
    {
        private static readonly Role[] ExemptionRoles = { Role.Operator, Role.Moderator, Role.ModbotExempt };

        private readonly ILogger<ModeBase> _logger;
        private readonly IImmutableDictionary<string, IChat> _chats;
        private readonly IImmutableDictionary<string, ICommandResponder> _commandResponders;
        private readonly IImmutableDictionary<string, CommandProcessor> _commandProcessors;
        private readonly IImmutableDictionary<string, IModerator> _moderators;
        private readonly IImmutableDictionary<string, AdvertisePollsWorker> _advertisePollsWorkers;
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
            Setups.Databases repos,
            BaseConfig baseConfig,
            StopToken stopToken,
            MuteInputsToken? muteInputsToken,
            OverlayConnection overlayConnection,
            ProcessMessage? processMessage = null)
        {
            IClock clock = SystemClock.Instance;
            _logger = loggerFactory.CreateLogger<ModeBase>();
            PokedexData pokedexData = PokedexData.Load();
            ArgsParser argsParser = Setups.SetUpArgsParser(repos.UserRepo, pokedexData);
            _processMessage = processMessage ?? (_ => Task.FromResult(false));

            var chats = new Dictionary<string, IChat>();
            var chatFactory = new ChatFactory(loggerFactory, clock,
                repos.UserRepo, repos.TokensBank, repos.SubscriptionLogRepo, repos.LinkedAccountRepo,
                overlayConnection);
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
                c => Setups.SetUpCommandProcessor(loggerFactory, argsParser, repos, stopToken, muteInputsToken,
                    messageSender: c, chatModeChanger: c, pokedexData.KnownSpecies));

            _messagequeueRepo = repos.MessagequeueRepo;
            _messagelogRepo = repos.MessagelogRepo;
            _forwardUnprocessedMessages = baseConfig.Chat.ForwardUnprocessedMessages;
            _clock = SystemClock.Instance;

            ILogger<Moderator> moderatorLogger = loggerFactory.CreateLogger<Moderator>();

            IImmutableList<IModerationRule> availableRules = ImmutableList.Create<IModerationRule>(
                new BannedUrlsRule(),
                new NewUserLinkRule(_clock),
                new EmoteRule(),
                new CopypastaRule(clock),
                new PersonalRepetitionRule(clock),
                new BannedWordsRule(baseConfig.ModbotBannedWords),
                new UnicodeCharacterCategoryRule()
            );
            foreach (string unknown in baseConfig.DisabledModbotRules.Except(availableRules.Select(rule => rule.Id)))
                moderatorLogger.LogWarning("unknown modbot rule '{UnknownRule}' marked as disabled", unknown);
            IImmutableList<IModerationRule> rules = availableRules
                .Where(rule => !baseConfig.DisabledModbotRules.Contains(rule.Id))
                .ToImmutableList();

            _moderators = _chats.Values.ToImmutableDictionary(
                c => c.Name,
                c => (IModerator)new Moderator(moderatorLogger, c, rules, repos.ModLogRepo, clock));
            _advertisePollsWorkers = _chats.Values.ToImmutableDictionary(
                c => c.Name,
                c => new AdvertisePollsWorker(baseConfig.AdvertisePollsInterval, repos.PollRepo, c));
        }

        public void InstallAdditionalCommand(Command command)
        {
            foreach (CommandProcessor commandProcessor in _commandProcessors.Values)
                commandProcessor.InstallCommand(command);
        }

        private void MessageReceived(object? sender, MessageEventArgs e)
        {
            TaskToVoidSafely(_logger, async () =>
            {
                await ProcessIncomingMessage((IChat)sender!, e.Message);
            });
        }

        private async Task ProcessIncomingMessage(IChat chat, Message message)
        {
            await _messagelogRepo.LogChat(
                message.User.Id, message.RawIrcMessage, message.MessageText, _clock.GetCurrentInstant());

            bool isOk = message.Details.IsStaff
                        || message.User.Roles.Intersect(ExemptionRoles).Any()
                        || message.MessageSource != MessageSource.Chat
                        || await _moderators[chat.Name].Check(message);
            if (!isOk)
            {
                return;
            }

            string cleanedMessage = message.MessageText;
            // The 7tv browser extension's setting "Allow sending the same message twice" (general.allow_send_twice)
            // appends ` \u{E0000}` to messages to bypass Twitch's "message is identical" notice. That is noise.
            if (cleanedMessage.EndsWith("\U000E0000")) cleanedMessage = cleanedMessage[..^2]; // 2 characters because it's a surrogate pair in UTF-16

            List<string> parts = cleanedMessage.Split(" ")
                .Where(s => !string.IsNullOrEmpty(s)).ToList();
            string? firstPart = parts.FirstOrDefault();
            string? commandName = firstPart switch
            {
                null => null,
                var name when message.MessageSource == MessageSource.Whisper
                    => name.StartsWith('!') ? name[1..] : name,
                var name when message.MessageSource == MessageSource.Chat && name.StartsWith('!')
                    => name[1..],
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
            wasProcessed |= await _processMessage(message);
            if (!wasProcessed && _forwardUnprocessedMessages)
            {
                await _messagequeueRepo.EnqueueMessage(message.RawIrcMessage);
            }
        }

        public void Start()
        {
            foreach (IChat chat in _chats.Values)
                chat.Connect();
            foreach (var advertisePollsWorker in _advertisePollsWorkers.Values)
                advertisePollsWorker.Start();
        }

        public void Dispose()
        {
            foreach (IChat chat in _chats.Values)
            {
                chat.Dispose();
                chat.IncomingMessage -= MessageReceived;
            }
            foreach (var advertisePollsWorker in _advertisePollsWorkers.Values)
                advertisePollsWorker.Dispose();
        }
    }
}
