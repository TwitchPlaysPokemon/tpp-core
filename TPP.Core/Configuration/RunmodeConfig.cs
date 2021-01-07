namespace TPP.Core.Configuration
{
    public class RunmodeConfig : ConfigBase, IRootConfig
    {
        public string Schema => "./config.runmode.schema.json";

        public InputConfig InputConfig { get; init; } = new();
    }
}
