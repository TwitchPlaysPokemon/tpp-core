using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NodaTime;
using TPP.ArgsParsing;
using TPP.ArgsParsing.TypeParsers;
using TPP.Common;
using TPP.Core.Chat;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Core.Configuration;
using TPP.Core.Moderation;
using TPP.Core.Overlay;
using TPP.Core.Overlay.Events;
using TPP.Inputting;
using TPP.Model;
using TPP.Persistence;
using TPP.Persistence.MongoDB;
using TPP.Persistence.MongoDB.Repos;
using TPP.Persistence.MongoDB.Serializers;
using static TPP.Core.EventUtils;

namespace TPP.Core
{
    /// <summary>
    /// Bundling up boilerplate code required to construct various classes.
    /// </summary>
    public static class Setups
    {
        public static ArgsParser SetUpArgsParser(IUserRepo userRepo, PokedexData pokedexData)
        {
            var argsParser = new ArgsParser();

            // simple parsers
            argsParser.AddArgumentParser(new BoolParser());
            argsParser.AddArgumentParser(new SignedIntParser());
            argsParser.AddArgumentParser(new PositiveIntParser());
            argsParser.AddArgumentParser(new NonNegativeIntParser());
            argsParser.AddArgumentParser(new StringParser());
            argsParser.AddArgumentParser(new InstantParser());
            argsParser.AddArgumentParser(new TimeSpanParser());
            argsParser.AddArgumentParser(new HexColorParser());
            argsParser.AddArgumentParser(new PokeyenParser());
            argsParser.AddArgumentParser(new TokensParser());
            argsParser.AddArgumentParser(new SignedPokeyenParser());
            argsParser.AddArgumentParser(new SignedTokensParser());
            argsParser.AddArgumentParser(new RoleParser());
            argsParser.AddArgumentParser(new PercentageParser());
            argsParser.AddArgumentParser(new SideParser());

            // generic parsers
            argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
            argsParser.AddArgumentParser(new OneOfParser(argsParser));
            argsParser.AddArgumentParser(new OptionalParser(argsParser));
            argsParser.AddArgumentParser(new ManyOfParser(argsParser));

            // parsers with dependencies
            argsParser.AddArgumentParser(new UserParser(userRepo));
            argsParser.AddArgumentParser(new PkmnSpeciesParser(pokedexData.KnownSpecies, PokedexData.NormalizeName));

            return argsParser;
        }

        public static ITransmuter SetUpTransmuter(
            ILoggerFactory loggerFactory,
            IImmutableSet<Common.PkmnSpecies> knownSpecies,
            Databases databases,
            OverlayConnection overlayConnection)
        {
            ImmutableSortedSet<PkmnSpecies> transmutableSpecies = knownSpecies
                .Where(s => s.GetGeneration()
                    is Generation.Gen1 or Generation.Gen2 or Generation.Gen3 or Generation.Gen4
                    or Generation.Gen5 or Generation.Gen6 or Generation.Gen7 or Generation.Gen8
                )
                .ToImmutableSortedSet();
            ITransmutationCalculator transmutationCalculator = new TransmutationCalculator(
                badgeStatsRepo: databases.BadgeStatsRepo,
                transmutableSpecies: transmutableSpecies,
                random: new Random().NextDouble);
            ITransmuter transmuter = new Transmuter(
                databases.BadgeRepo, transmutationCalculator, databases.TokensBank, databases.TransmutationLogRepo,
                SystemClock.Instance);
            transmuter.Transmuted += (_, evt) => TaskToVoidSafely(loggerFactory.CreateLogger<ITransmuter>(), async () =>
            {
                TransmuteEvent overlayEvent = new(evt.User.Name, evt.InputSpecies, evt.OutputSpecies, evt.Candidates);
                await overlayConnection.Send(overlayEvent, CancellationToken.None);
            });
            return transmuter;
        }

        public static CommandProcessor SetUpCommandProcessor(
            ILoggerFactory loggerFactory,
            BaseConfig config,
            ArgsParser argsParser,
            Databases databases,
            StopToken stopToken,
            MuteInputsToken? muteInputsToken,
            IMessageSender messageSender,
            IChatModeChanger chatModeChanger,
            IExecutor executor,
            IImmutableSet<Common.PkmnSpecies> knownSpecies,
            ITransmuter transmuter)
        {
            var commandProcessor = new CommandProcessor(
                loggerFactory.CreateLogger<CommandProcessor>(),
                databases.CommandLogger, argsParser);

            var moderationService = new ModerationService(
                SystemClock.Instance, executor, databases.TimeoutLogRepo, databases.BanLogRepo, databases.UserRepo);
            ILogger<ModerationService> logger = loggerFactory.CreateLogger<ModerationService>();
            moderationService.ModerationActionPerformed += (_, args) => TaskToVoidSafely(logger, () =>
            {
                // TODO matchmode: clear actions
                //      Note: Already works with old core because it watches the "timeoutlog" collection.

                // TODO deputy stuff
                return Task.CompletedTask;
            });

            List<Command> commands = new[]
            {
                new EasterEggCommands().Commands,
                new StaticResponseCommands().Commands,
                new MiscCommands(SystemClock.Instance).Commands,
                new UserCommands(databases.UserRepo).Commands,
                new TransmuteCommands(transmuter).Commands,
                new OperatorCommands(
                    stopToken, muteInputsToken, databases.PokeyenBank, databases.TokensBank,
                    messageSender: messageSender, databases.BadgeRepo, databases.UserRepo, databases.InputSidePicksRepo
                ).Commands,
                new ModeratorCommands(
                    chatModeChanger, databases.LinkedAccountRepo, databases.ResponseCommandRepo
                ).Commands,
                new ModerationCommands(
                    moderationService, databases.BanLogRepo, databases.TimeoutLogRepo, databases.UserRepo,
                    SystemClock.Instance
                ).Commands
            }.SelectMany(cmds => cmds).ToList();
            if (!config.DisabledFeatures.Contains(TppFeatures.Badges))
            {
                commands.AddRange(new BadgeCommands(
                    databases.BadgeRepo, databases.BadgeStatsRepo, databases.UserRepo, messageSender, knownSpecies
                ).Commands);
            }
            if (!config.DisabledFeatures.Contains(TppFeatures.Currencies))
            {
                commands.AddRange(new CurrencyCommands(
                    pokeyenBank: databases.PokeyenBank, tokenBank: databases.TokensBank, messageSender).Commands);
            }
            if (!config.DisabledFeatures.Contains(TppFeatures.Polls))
            {
                commands.AddRange(new PollCommands(databases.PollRepo).Commands);
                commands.AddRange(new ManagePollCommands(databases.PollRepo).Commands);
            }
            if (!config.DisabledFeatures.Contains(TppFeatures.Cosmetics))
            {
                commands.AddRange(new CosmeticsCommands(databases.UserRepo, tokenBank: databases.TokensBank).Commands);
            }

            commands.Add(new HelpCommand(commandProcessor).Command);
            foreach (Command command in commands)
            {
                commandProcessor.InstallCommand(command);
            }
            SetUpDynamicCommands(loggerFactory.CreateLogger("setups"), commandProcessor, databases.ResponseCommandRepo);
            return commandProcessor;
        }

        public record Databases(
            IUserRepo UserRepo,
            IPollRepo PollRepo,
            IBadgeRepo BadgeRepo,
            IBadgeStatsRepo BadgeStatsRepo,
            IBank<User> PokeyenBank,
            IBank<User> TokensBank,
            ICommandLogger CommandLogger,
            IOutgoingMessagequeueRepo OutgoingMessagequeueRepo,
            IIncomingMessagequeueRepo IncomingMessagequeueRepo,
            IMessagelogRepo MessagelogRepo,
            ILinkedAccountRepo LinkedAccountRepo,
            ISubscriptionLogRepo SubscriptionLogRepo,
            IModbotLogRepo ModbotLogRepo,
            IBanLogRepo BanLogRepo,
            ITimeoutLogRepo TimeoutLogRepo,
            IResponseCommandRepo ResponseCommandRepo,
            IRunCounterRepo RunCounterRepo,
            IInputLogRepo InputLogRepo,
            IInputSidePicksRepo InputSidePicksRepo,
            KeyValueStore KeyValueStore,
            ITransmutationLogRepo TransmutationLogRepo,
            IChattersSnapshotsRepo ChattersSnapshotsRepo
        );

        public static Databases SetUpRepositories(ILoggerFactory loggerFactory, ILogger logger, BaseConfig baseConfig)
        {
            IClock clock = SystemClock.Instance;
            CustomSerializers.RegisterAll();
            MongoClientSettings settings = MongoClientSettings.FromConnectionString(baseConfig.MongoDbConnectionUri);
            settings.LinqProvider = LinqProvider.V3;
            IMongoClient mongoClient = new MongoClient(settings);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(baseConfig.MongoDbDatabaseName);
            IMongoDatabase mongoDatabaseMessagelog = mongoClient.GetDatabase(baseConfig.MongoDbDatabaseNameMessagelog);
            UserRepo userRepo = new(
                database: mongoDatabase,
                startingPokeyen: baseConfig.StartingPokeyen,
                startingTokens: baseConfig.StartingTokens,
                defaultOperators: baseConfig.Chat.DefaultOperatorNames,
                clock: clock);
            IMongoBadgeLogRepo badgeLogRepo = new BadgeLogRepo(mongoDatabase);
            BadgeRepo badgeRepo = new(mongoDatabase, badgeLogRepo, clock);
            badgeRepo.UserLostBadgeSpecies += (_, args) => TaskToVoidSafely(logger, () =>
                userRepo.UnselectBadgeIfSpeciesSelected(args.UserId, args.Species));
            IBank<User> pokeyenBank = new Bank<User>(
                database: mongoDatabase,
                currencyCollectionName: UserRepo.CollectionName,
                transactionLogCollectionName: "pokeyentransactions",
                u => u.Pokeyen,
                u => u.Id,
                clock: clock);
            IBank<User> tokenBank = new Bank<User>(
                database: mongoDatabase,
                currencyCollectionName: UserRepo.CollectionName,
                transactionLogCollectionName: "tokentransactions",
                u => u.Tokens,
                u => u.Id,
                clock: clock);
            tokenBank.AddReservedMoneyChecker(
                new PersistedReservedMoneyCheckers(mongoDatabase).AllDatabaseReservedTokens);
            return new Databases
            (
                UserRepo: userRepo,
                BadgeRepo: badgeRepo,
                BadgeStatsRepo: badgeRepo,
                PollRepo: new PollRepo(mongoDatabase, clock),
                PokeyenBank: pokeyenBank,
                TokensBank: tokenBank,
                CommandLogger: new CommandLogger(mongoDatabase, clock),
                OutgoingMessagequeueRepo: new OutgoingMessagequeueRepo(mongoDatabase),
                IncomingMessagequeueRepo: new IncomingMessagequeueRepo(
                    mongoDatabase, loggerFactory.CreateLogger<IncomingMessagequeueRepo>()),
                MessagelogRepo: new MessagelogRepo(mongoDatabaseMessagelog),
                LinkedAccountRepo: new LinkedAccountRepo(mongoDatabase, userRepo.Collection),
                SubscriptionLogRepo: new SubscriptionLogRepo(mongoDatabase),
                ModbotLogRepo: new ModbotLogRepo(mongoDatabase),
                BanLogRepo: new BanLogRepo(mongoDatabase),
                TimeoutLogRepo: new TimeoutLogRepo(mongoDatabase),
                ResponseCommandRepo: new ResponseCommandRepo(mongoDatabase),
                RunCounterRepo: new RunCounterRepo(mongoDatabase),
                InputLogRepo: new InputLogRepo(mongoDatabase),
                InputSidePicksRepo: new InputSidePicksRepo(mongoDatabase, clock),
                KeyValueStore: new KeyValueStore(mongoDatabase),
                TransmutationLogRepo: new TransmutationLogRepo(mongoDatabase),
                ChattersSnapshotsRepo: new ChattersSnapshotsRepo(mongoDatabase)
            );
        }

        public static (WebsocketBroadcastServer, OverlayConnection) SetUpOverlayServer(
            ILoggerFactory loggerFactory, string wsHost, int wsPort)
        {
            WebsocketBroadcastServer broadcastServer = new(
                loggerFactory.CreateLogger<WebsocketBroadcastServer>(), wsHost, wsPort);
            OverlayConnection overlayConnection = new(
                loggerFactory.CreateLogger<OverlayConnection>(), broadcastServer);
            return (broadcastServer, overlayConnection);
        }

        private static void SetUpDynamicCommands(
            ILogger logger, CommandProcessor commandProcessor, IResponseCommandRepo responseCommandRepo)
        {
            IImmutableList<ResponseCommand> commands = responseCommandRepo.GetCommands().Result;

            HashSet<string> dynamicallyInstalledCommands = new();
            void InstallCommand(ResponseCommand command)
            {
                Command? existing = commandProcessor.FindCommand(command.Command);
                if (existing != null)
                {
                    logger.LogWarning(
                        "not installing static response command '{Command}' " +
                        "because it conflicts with an existing command", command.Command);
                }
                else
                {
                    commandProcessor
                        .InstallCommand(new Command(command.Command, CommandUtils.StaticResponse(command.Response)));
                    dynamicallyInstalledCommands.Add(command.Command);
                }
            }
            void UninstallCommand(string commandName)
            {
                if (!dynamicallyInstalledCommands.Contains(commandName))
                    return; // this command wasn't added dynamically, probably because it conflicted
                commandProcessor.UninstallCommand(commandName);
                dynamicallyInstalledCommands.Remove(commandName);
            }

            foreach (ResponseCommand command in commands)
            {
                InstallCommand(command);
            }
            responseCommandRepo.CommandRemoved += (_, name) => UninstallCommand(name);
            responseCommandRepo.CommandInserted += (_, command) => InstallCommand(command);
        }
    }
}
