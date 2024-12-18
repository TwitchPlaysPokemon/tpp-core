using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
using TPP.Core.Streamlabs;
using TPP.Core.Utils;
using TPP.Inputting;
using TPP.Model;
using TPP.Persistence;
using static TPP.Core.EventUtils;
using static TPP.Core.MessageSource;

namespace TPP.Core.Modes
{
    public sealed class ModeBase : IWithLifecycle, ICommandHandler
    {
        private static readonly Role[] ExemptionRoles = { Role.Operator, Role.Moderator, Role.ModbotExempt };

        private readonly ILogger<ModeBase> _logger;
        private readonly IImmutableDictionary<string, IChat> _chats;
        private readonly IImmutableDictionary<string, ICommandResponder> _commandResponders;
        private readonly IImmutableDictionary<string, CommandProcessor> _commandProcessors;
        private readonly IImmutableDictionary<string, IModerator> _moderators;
        private readonly IImmutableDictionary<string, AdvertisePollsWorker>? _advertisePollsWorkers;
        private readonly SendOutQueuedMessagesWorker? _sendOutQueuedMessagesWorker;
        private readonly IOutgoingMessagequeueRepo _outgoingMessagequeueRepo;
        private readonly bool _forwardUnprocessedMessages;
        private readonly IMessagelogRepo _messagelogRepo;
        private readonly IClock _clock;
        private readonly ProcessMessage _processMessage;
        private readonly ChattersWorker? _chattersWorker;
        private readonly DonationsWorker? _donationsWorker;

        /// Processes a message that wasn't already processed by the mode base,
        /// and returns whether the message was actively processed.
        public delegate Task<bool> ProcessMessage(IChat chat, Message message);

        public ModeBase(
            ILoggerFactory loggerFactory,
            Setups.Databases repos,
            BaseConfig baseConfig,
            IStopToken stopToken,
            MuteInputsToken? muteInputsToken,
            OverlayConnection overlayConnection,
            ProcessMessage? processMessage = null)
        {
            IClock clock = SystemClock.Instance;
            _logger = loggerFactory.CreateLogger<ModeBase>();
            PokedexData pokedexData = PokedexData.Load();
            ArgsParser argsParser = Setups.SetUpArgsParser(repos.UserRepo, pokedexData);
            _processMessage = processMessage ?? ((_, _) => Task.FromResult(false));

            var chats = new Dictionary<string, IChat>();
            var chatFactory = new ChatFactory(loggerFactory, clock,
                repos.UserRepo, repos.CoStreamChannelsRepo, repos.TokensBank, repos.SubscriptionLogRepo,
                repos.LinkedAccountRepo, overlayConnection);
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
            ITransmuter transmuter = Setups.SetUpTransmuter(
                loggerFactory, pokedexData.KnownSpecies, repos, overlayConnection);
            _commandProcessors = _chats.Values.ToImmutableDictionary(
                c => c.Name,
                c => Setups.SetUpCommandProcessor(loggerFactory, baseConfig, argsParser, repos, stopToken,
                    muteInputsToken, messageSender: c,
                    chatModeChanger: c as IChatModeChanger, executor: c as IExecutor,
                    pokedexData.KnownSpecies, transmuter, this));

            _outgoingMessagequeueRepo = repos.OutgoingMessagequeueRepo;
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

            _moderators = _chats.Values
                .Where(c => c is IExecutor)
                .ToImmutableDictionary(
                    c => c.Name,
                    c => (IModerator)new Moderator(moderatorLogger, (IExecutor)c, rules, repos.ModbotLogRepo, clock));
            if (!baseConfig.DisabledFeatures.Contains(TppFeatures.Polls))
                _advertisePollsWorkers = _chats.Values.ToImmutableDictionary(
                    c => c.Name,
                    c => new AdvertisePollsWorker(loggerFactory.CreateLogger<AdvertisePollsWorker>(),
                        baseConfig.AdvertisePollsInterval, repos.PollRepo, c));

            if (baseConfig.Chat.SendOutForwardedMessages)
            {
                if (!_chats.Any())
                {
                    _logger.LogError("sending out forwarded messages is enabled, but no chat is configured!");
                }
                else
                {
                    (string chatName, IChat chat) = _chats.First();
                    if (_chats.Count > 1)
                        _logger.LogWarning(
                            "Multiple chats configured, using {Chat} for sending out of forwarded messages", chatName);

                    _sendOutQueuedMessagesWorker = new SendOutQueuedMessagesWorker(
                        loggerFactory.CreateLogger<SendOutQueuedMessagesWorker>(),
                        repos.IncomingMessagequeueRepo,
                        repos.UserRepo,
                        chat, clock);
                }
            }

            List<ConnectionConfig.Twitch> chatsWithChattersWorker = baseConfig.Chat.Connections
                .OfType<ConnectionConfig.Twitch>()
                .Where(con => con.GetChattersInterval != null)
                .ToList();
            ConnectionConfig.Twitch? primaryChat = chatsWithChattersWorker.FirstOrDefault();
            if (chatsWithChattersWorker.Count > 1)
                _logger.LogWarning("More than one twitch chat have GetChattersInterval configured: {ChatNames}. " +
                                   "Using only the first one ('{ChosenChat}') for the chatters worker",
                    string.Join(", ", chatsWithChattersWorker.Select(c => c.Name)), primaryChat?.Name);
            _chattersWorker = primaryChat == null
                ? null
                : new ChattersWorker(loggerFactory, clock,
                    ((TwitchChat)_chats[primaryChat.Name]).TwitchApi, repos.ChattersSnapshotsRepo, primaryChat);

            StreamlabsConfig streamlabsConfig = baseConfig.StreamlabsConfig;
            if (streamlabsConfig.Enabled)
            {
                IChat chat = _chats.Values.First(); // TODO
                DonationHandler donationHandler = new(loggerFactory.CreateLogger<DonationHandler>(),
                    repos.DonationRepo, repos.UserRepo, repos.TokensBank, chat, overlayConnection,
                    baseConfig.DonorBadgeCents);
                StreamlabsClient streamlabsClient = new(loggerFactory.CreateLogger<StreamlabsClient>(),
                    streamlabsConfig.AccessToken);
                _donationsWorker = new DonationsWorker(loggerFactory, streamlabsConfig.PollingInterval,
                    streamlabsClient, repos.DonationRepo, donationHandler);
            }
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

        private static readonly CaseInsensitiveImmutableHashSet CoStreamAllowedCommands =
            new(["left", "right"]);
        public async Task ProcessIncomingMessage(IChat chat, Message message)
        {
            Instant now = _clock.GetCurrentInstant();
            await _messagelogRepo.LogChat(
                message.User.Id, message.RawIrcMessage, message.MessageText, now);

            bool isOk = message.Details.IsStaff
                        || message.User.Roles.Intersect(ExemptionRoles).Any()
                        || message.MessageSource is not PrimaryChat
                        || !_moderators.TryGetValue(chat.Name, out IModerator? moderator)
                        || await moderator.Check(message);
            if (!isOk)
            {
                return;
            }
            if (message.User.Banned || message.User.TimeoutExpiration > now)
            {
                _logger.LogDebug("Skipping message from {User} because user is banned or timed out", message.User);
                return;
            }

            string cleanedMessage = message.MessageText;
            // The 7tv browser extension's setting "Allow sending the same message twice" (general.allow_send_twice)
            // appends ` \u{E0000}` to messages to bypass Twitch's "message is identical" notice. That is noise.
            if (cleanedMessage.EndsWith("\U000E0000"))
                cleanedMessage = cleanedMessage[..^2]; // 2 characters because it's a surrogate pair in UTF-16

            List<string> parts = cleanedMessage.Split(" ")
                .Where(s => !string.IsNullOrEmpty(s)).ToList();
            string? firstPart = parts.FirstOrDefault();
            string? commandName = firstPart switch
            {
                null => null,
                _ when message.MessageSource is Whisper
                    => firstPart.StartsWith('!') ? firstPart[1..] : firstPart,
                _ when message.MessageSource is PrimaryChat && firstPart.StartsWith('!')
                    => firstPart[1..],
                _ when message.MessageSource is SecondaryChat && firstPart.StartsWith('!')
                                                              && CoStreamAllowedCommands.Contains(firstPart[1..])
                    => firstPart[1..],
                _ => null
            };
            bool wasProcessed = false;
            if (commandName != null)
            {
                CommandResult? result = await _commandProcessors[chat.Name]
                    .Process(commandName, parts.Skip(1).ToImmutableList(), message, chat);
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
            wasProcessed |= await _processMessage(chat, message);
            if (!wasProcessed && _forwardUnprocessedMessages && message.MessageSource is PrimaryChat or Whisper)
            {
                await _outgoingMessagequeueRepo.EnqueueMessage(message.RawIrcMessage);
            }
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            List<Task> tasks = [];
            tasks.AddRange(_chats.Values.Select(chat => chat.Start(cancellationToken)));
            if (_advertisePollsWorkers != null)
                tasks.AddRange(_advertisePollsWorkers.Values.Select(worker => worker.Start(cancellationToken)));
            if (_sendOutQueuedMessagesWorker != null)
                tasks.Add(_sendOutQueuedMessagesWorker.Start(cancellationToken));
            if (_chattersWorker != null)
                tasks.Add(_chattersWorker.Start(cancellationToken));
            if (_donationsWorker != null)
                tasks.Add(_donationsWorker.Start(cancellationToken));
            await TaskUtils.WhenAllFastExit(tasks);

            foreach (IChat chat in _chats.Values)
                chat.IncomingMessage -= MessageReceived;
        }
    }
}
