using Newtonsoft.Json;

namespace Core.Configuration
{
    public class MatchmodeConfig : ConfigBase
    {
        [JsonProperty("$schema")] public static string Schema { get; private set; } = "./config.matchmode.schema.json";
    }
}
