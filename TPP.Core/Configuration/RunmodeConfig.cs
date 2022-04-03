namespace TPP.Core.Configuration
{
    public class RunmodeConfig : ConfigBase, IRootConfig
    {
        public string Schema => "./config.runmode.schema.json";

        public string InputServerHost { get; init; } = "127.0.0.1";
        public int InputServerPort { get; init; } = 5010;

        public InputConfig InputConfig { get; init; } = new();

        /* If this is not null, any user participating will get that run number's participation emblem. */
        public int? RunNumber = null;
    }
}
