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
using TPP.Model;
using TPP.Persistence;
using TPP.Persistence.MongoDB;
using TPP.Persistence.MongoDB.Repos;
using TPP.Persistence.MongoDB.Serializers;
using static TPP.Core.EventUtils;

namespace TPP.Core;

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

    public static CommandProcessor SetUpCommandProcessor(
        ILoggerFactory loggerFactory,
        ArgsParser argsParser,
        Databases databases,
        StopToken stopToken,
        IMessageSender messageSender,
        IChatModeChanger chatModeChanger,
        IImmutableSet<Common.PkmnSpecies> knownSpecies)
    {
        var commandProcessor = new CommandProcessor(
            loggerFactory.CreateLogger<CommandProcessor>(),
            databases.CommandLogger, argsParser);

        IEnumerable<Command> commands = new[]
        {
            new EasterEggCommands().Commands,
            new StaticResponseCommands().Commands,
            new MiscCommands().Commands,
            new UserCommands(
                databases.UserRepo, pokeyenBank: databases.PokeyenBank, tokenBank: databases.TokensBank,
                messageSender
            ).Commands,
            new PollCommands(databases.PollRepo).Commands,
            new ManagePollCommands(databases.PollRepo).Commands,
            new BadgeCommands(databases.BadgeRepo, databases.UserRepo, messageSender, knownSpecies).Commands,
            new OperatorCommands(
                stopToken, databases.PokeyenBank, databases.TokensBank,
                messageSender: messageSender, databases.BadgeRepo, databases.UserRepo
            ).Commands,
            new ModeratorCommands(
                chatModeChanger, databases.LinkedAccountRepo, databases.ResponseCommandRepo
            ).Commands
        }.SelectMany(cmds => cmds).Concat(new[]
        {
            new HelpCommand(commandProcessor).Command
        });
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
        IBank<User> PokeyenBank,
        IBank<User> TokensBank,
        ICommandLogger CommandLogger,
        IMessagequeueRepo MessagequeueRepo,
        IMessagelogRepo MessagelogRepo,
        ILinkedAccountRepo LinkedAccountRepo,
        ISubscriptionLogRepo SubscriptionLogRepo,
        IModLogRepo ModLogRepo,
        IResponseCommandRepo ResponseCommandRepo,
        KeyValueStore KeyValueStore
    );

    public static Databases SetUpRepositories(ILogger logger, BaseConfig baseConfig)
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
            defaultOperators: baseConfig.Chat.DefaultOperatorNames,
            clock: clock);
        IMongoBadgeLogRepo badgeLogRepo = new BadgeLogRepo(mongoDatabase);
        IBadgeRepo badgeRepo = new BadgeRepo(mongoDatabase, badgeLogRepo, clock);
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
            PollRepo: new PollRepo(mongoDatabase, clock),
            PokeyenBank: pokeyenBank,
            TokensBank: tokenBank,
            CommandLogger: new CommandLogger(mongoDatabase, clock),
            MessagequeueRepo: new MessagequeueRepo(mongoDatabase),
            MessagelogRepo: new MessagelogRepo(mongoDatabaseMessagelog),
            LinkedAccountRepo: new LinkedAccountRepo(mongoDatabase, userRepo.Collection),
            SubscriptionLogRepo: new SubscriptionLogRepo(mongoDatabase),
            ModLogRepo: new ModLogRepo(mongoDatabase),
            ResponseCommandRepo: new ResponseCommandRepo(mongoDatabase),
            KeyValueStore: new KeyValueStore(mongoDatabase)
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
