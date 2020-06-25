using Newtonsoft.Json;

namespace Core.Configuration
{
    /// <summary>
    /// The root of TPP-Core-Configuration.
    /// Contains all other configuration nodes.
    /// </summary>
    public sealed class RootConfig : ConfigBase
    {
        [JsonProperty("$schema")] public static string Schema { get; } = "./config.schema.json";

        /* directory under which log files will be created. null for no log files. */
        public string? LogPath { get; init; } = null;

        /* connection details for mongodb */
        public string MongoDbConnectionUri { get; init; } = "mongodb://localhost:27017/?replicaSet=rs0";
        public string MongoDbDatabaseName { get; init; } = "tpp3";

        public IrcConfig Irc { get; init; } = new IrcConfig();

        /* currency amounts for brand new users (new entries in the database) */
        public int StartingPokeyen { get; init; } = 100;
        public int StartingTokens { get; init; } = 0;
    }
}
