using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NodaTime;
using TPP.ArgsParsing;
using TPP.ArgsParsing.TypeParsers;
using TPP.Core.Chat;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Core.Configuration;
using TPP.Core.Overlay;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB;
using TPP.Persistence.MongoDB.Repos;
using TPP.Persistence.MongoDB.Serializers;
using TPP.Persistence.Repos;

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
            argsParser.AddArgumentParser(new PkmnSpeciesParser(pokedexData.KnownSpecies, PokedexData.NormalizeName));
            argsParser.AddArgumentParser(new PercentageParser());
            argsParser.AddArgumentParser(new SideParser());

            argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
            argsParser.AddArgumentParser(new OneOfParser(argsParser));
            argsParser.AddArgumentParser(new OptionalParser(argsParser));
            argsParser.AddArgumentParser(new ManyOfParser(argsParser));

            argsParser.AddArgumentParser(new UserParser(userRepo));
            return argsParser;
        }

        public static CommandProcessor SetUpCommandProcessor(
            ILoggerFactory loggerFactory,
            ArgsParser argsParser,
            Databases databases,
            StopToken stopToken,
            ChatConfig chatConfig,
            IMessageSender messageSender,
            IChatModeChanger chatModeChanger,
            IImmutableSet<Common.PkmnSpecies> knownSpecies)
        {
            var commandProcessor = new CommandProcessor(
                loggerFactory.CreateLogger<CommandProcessor>(),
                databases.CommandLogger, argsParser,
                chatConfig.DefaultOperatorNames);

            IEnumerable<Command> commands = new[]
            {
                new EasterEggCommands().Commands,
                new StaticResponseCommands().Commands,
                new UserCommands(
                    databases.UserRepo, pokeyenBank: databases.PokeyenBank, tokenBank: databases.TokensBank,
                    messageSender
                ).Commands,
                new PollCommands(databases.PollRepo).Commands,
                new CreatePollCommands(databases.PollRepo).Commands,
                new BadgeCommands(databases.BadgeRepo, databases.UserRepo, messageSender, knownSpecies).Commands,
                new OperatorCommands(
                    stopToken,chatConfig.DefaultOperatorNames, databases.PokeyenBank, databases.TokensBank,
                    messageSender: messageSender, databases.BadgeRepo, databases.UserRepo
                ).Commands,
                new ModeratorCommands(chatModeChanger, databases.LinkedAccountRepo).Commands
            }.SelectMany(cmds => cmds).Concat(new[]
            {
                new HelpCommand(commandProcessor).Command
            });
            foreach (Command command in commands)
            {
                commandProcessor.InstallCommand(command);
            }
            return commandProcessor;
        }

        public record Databases(
            IUserRepo UserRepo,
            IPollRepo PollRepo,
            IBadgeRepo BadgeRepo,
            IBank<User> PokeyenBank,
            IBank<User> TokensBank,
            ICommandLogger CommandLogger,
            IMessagequeueRepo MessagequeueRepo,
            IMessagelogRepo MessagelogRepo,
            ILinkedAccountRepo LinkedAccountRepo,
            ISubscriptionLogRepo SubscriptionLogRepo,
            IModLogRepo ModLogRepo
        );

        public static Databases SetUpRepositories(BaseConfig baseConfig)
        {
            IClock clock = SystemClock.Instance;
            CustomSerializers.RegisterAll();
            IMongoClient mongoClient = new MongoClient(baseConfig.MongoDbConnectionUri);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(baseConfig.MongoDbDatabaseName);
            IMongoDatabase mongoDatabaseMessagelog = mongoClient.GetDatabase(baseConfig.MongoDbDatabaseNameMessagelog);
            UserRepo userRepo = new(
                database: mongoDatabase,
                startingPokeyen: baseConfig.StartingPokeyen,
                startingTokens: baseConfig.StartingTokens,
                defaultOperators: baseConfig.Chat.DefaultOperatorNames);
            IMongoBadgeLogRepo badgeLogRepo = new BadgeLogRepo(mongoDatabase);
            IBadgeRepo badgeRepo = new BadgeRepo(mongoDatabase, badgeLogRepo, clock);
            badgeRepo.UserLostBadgeSpecies += async (_, args) =>
                await userRepo.UnselectBadgeIfSpeciesSelected(args.UserId, args.Species);
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
                PollRepo: new PollRepo(mongoDatabase, clock),
                PokeyenBank: pokeyenBank,
                TokensBank: tokenBank,
                CommandLogger: new CommandLogger(mongoDatabase, clock),
                MessagequeueRepo: new MessagequeueRepo(mongoDatabase),
                MessagelogRepo: new MessagelogRepo(mongoDatabaseMessagelog),
                LinkedAccountRepo: new LinkedAccountRepo(mongoDatabase, userRepo.Collection),
                SubscriptionLogRepo: new SubscriptionLogRepo(mongoDatabase),
                ModLogRepo: new ModLogRepo(mongoDatabase)
            );
        }

        public static (WebsocketBroadcastServer, OverlayConnection) SetUpOverlayServer(ILoggerFactory loggerFactory)
        {
            (string wsHost, int wsPort) = ("localhost", 5001);
            WebsocketBroadcastServer broadcastServer = new(
                loggerFactory.CreateLogger<WebsocketBroadcastServer>(), wsHost, wsPort);
            OverlayConnection overlayConnection = new(
                loggerFactory.CreateLogger<OverlayConnection>(), broadcastServer);
            return (broadcastServer, overlayConnection);
        }
    }
}
