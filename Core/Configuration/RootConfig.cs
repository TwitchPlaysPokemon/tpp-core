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

        public IrcConfig Irc { get; private set; } = new IrcConfig();
    }
}
