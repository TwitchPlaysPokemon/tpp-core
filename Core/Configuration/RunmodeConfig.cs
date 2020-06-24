using Newtonsoft.Json;

namespace Core.Configuration
{
    public class RunmodeConfig : ConfigBase
    {
        [JsonProperty("$schema")] public static string Schema { get; private set; } = "./config.runmode.schema.json";
    }
}
