using Newtonsoft.Json;

namespace Core.Configuration
{
    /// <summary>
    /// The root of TPP-Core-Configuration.
    /// Contains all other configuration nodes.
    /// </summary>
    // properties need setters for deserialization
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    public sealed class RootConfig : ConfigBase
    {
        [JsonProperty("$schema")] public static string Schema { get; private set; } = "./config.schema.json";

        /* directory under which log files will be created. null for no log files. */
        public string? LogPath { get; private set; } = null;

        /* connection details for mongodb */
        public string MongoDbConnectionUri { get; private set; } = "mongodb://localhost:27017/?replicaSet=rs0";
        public string MongoDbDatabaseName { get; private set; } = "tpp3";

        public IrcConfig Irc { get; private set; } = new IrcConfig();

        /* currency amounts for brand new users (new entries in the database) */
        public int StartingPokeyen { get; private set; } = 100;
        public int StartingTokens { get; private set; } = 0;
    }
}
