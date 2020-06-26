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

        public IrcConfig Irc { get; private set; } = new IrcConfig();
    }
}
