namespace TPP.Core.Configuration
{
    public class RunmodeConfig : ConfigBase, IRootConfig
    {
        public string Schema => "./config.runmode.schema.json";
    }
}
