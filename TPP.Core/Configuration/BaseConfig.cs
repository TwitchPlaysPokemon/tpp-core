using System.Collections.Immutable;
using System.ComponentModel;
using NodaTime;
using Serilog.Events;

namespace TPP.Core.Configuration;

public enum TppFeatures
{
    Badges,
    Currencies,
    Polls,
    Cosmetics,
}

/// <summary>
/// The root of TPP-Core-Configuration.
/// Contains all configurations shared between all modes.
/// </summary>
public sealed class BaseConfig : ConfigBase, IRootConfig
{
    public string Schema => "./config.schema.json";

    [Description("Directory under which log files will be created. Use `null` for no log files.")]
    public string? LogPath { get; init; } = null;

    /* connection details for mongodb */
    public string MongoDbConnectionUri { get; init; } = "mongodb://localhost:27017/?replicaSet=rs0";
    public string MongoDbDatabaseName { get; init; } = "tpp3";
    public string MongoDbDatabaseNameMessagelog { get; init; } = "tpp3_messagelog";

    public ChatConfig Chat { get; init; } = new ChatConfig();

    [Description("Amount of pokeyen for brand new users (new entries in the database).")]
    public int StartingPokeyen { get; init; } = 100;
    [Description("Amount of tokens for brand new users (new entries in the database).")]
    public int StartingTokens { get; init; } = 0;

    [Description("Host of the HTTP server one may connect to to get overlay events through a websocket.")]
    public string OverlayWebsocketHost { get; init; } = "127.0.0.1";
    [Description("Port of the HTTP server one may connect to to get overlay events through a websocket.")]
    public int OverlayWebsocketPort { get; init; } = 5001;

    [Description("Required information to post log messages to discord. Logging to discord is disabled if null.")]
    public DiscordLoggingConfig? DiscordLoggingConfig { get; init; } = null;

    public ImmutableHashSet<string> DisabledModbotRules { get; init; } = ImmutableHashSet.Create<string>();
    public ImmutableHashSet<string> ModbotBannedWords { get; init; } = ImmutableHashSet.Create<string>();

    public Duration AdvertisePollsInterval { get; init; } = Duration.FromHours(1);

    public ImmutableHashSet<TppFeatures> DisabledFeatures { get; init; } = ImmutableHashSet<TppFeatures>.Empty;

    [Description("Donation handling via Streamlabs")]
    public StreamlabsConfig StreamlabsConfig { get; init; } = new();

    public int DonorBadgeCents { get; init; } = 20000;
}

/// <summary>
/// Configurations related to chat communication.
/// </summary>
public sealed class ChatConfig : ConfigBase
{
    public IImmutableList<string> DefaultOperatorNames { get; init; } = ImmutableList.Create("admin");

    public IImmutableList<ConnectionConfig> Connections { get; init; } =
        ImmutableList.Create<ConnectionConfig>(
            new ConnectionConfig.Console(),
            new ConnectionConfig.Twitch(),
            new ConnectionConfig.Simulation()
        );

    /* whether to forward unprocessed messages to the old core by saving them to the "messagequeue" collection */
    public bool ForwardUnprocessedMessages { get; init; } = true;

    /* whether to send out messages forwarded from the old core through the "messagequeue_in" collection */
    public bool SendOutForwardedMessages { get; init; } = true;
}

public sealed class DiscordLoggingConfig : ConfigBase
{
    public ulong WebhookId { get; init; } = 0L;
    public string WebhookToken { get; init; } = "";
    public LogEventLevel MinLogLevel { get; init; } = LogEventLevel.Warning;
}

public sealed class StreamlabsConfig : ConfigBase
{
    public bool Enabled { get; init; } = false;
    public string AccessToken { get; init; } = "";
}
