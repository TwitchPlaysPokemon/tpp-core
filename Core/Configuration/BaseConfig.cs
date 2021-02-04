namespace Core.Configuration
{
    /// <summary>
    /// The root of TPP-Core-Configuration.
    /// Contains all configurations shared between all modes.
    /// </summary>
    public sealed class BaseConfig : ConfigBase, IRootConfig
    {
        public string Schema => "./config.schema.json";

        /* directory under which log files will be created. null for no log files. */
        public string? LogPath { get; init; } = null;

        /* connection details for mongodb */
        public string MongoDbConnectionUri { get; init; } = "mongodb://localhost:27017/?replicaSet=rs0";
        public string MongoDbDatabaseName { get; init; } = "tpp3";
        public string MongoDbDatabaseNameMessagelog { get; init; } = "tpp3_messagelog";

        public ChatConfig Chat { get; init; } = new ChatConfig();

        /* currency amounts for brand new users (new entries in the database) */
        public int StartingPokeyen { get; init; } = 100;
        public int StartingTokens { get; init; } = 0;

        /* Required information to post log messages to discord. Logging to discord is disabled if null. */
        public DiscordLoggingConfig? DiscordLoggingConfig { get; init; } = null;
    }
}
