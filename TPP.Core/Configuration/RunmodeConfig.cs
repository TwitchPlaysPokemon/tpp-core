using System.ComponentModel;

namespace TPP.Core.Configuration
{
    public class RunmodeConfig : ConfigBase, IRootConfig
    {
        public string Schema => "./config.runmode.schema.json";

        [Description("Host of the HTTP input server where inputs can be polled from.")]
        public string InputServerHost { get; init; } = "127.0.0.1";
        [Description("Port of the HTTP input server where inputs can be polled from.")]
        public int InputServerPort { get; init; } = 5010;

        public InputConfig InputConfig { get; init; } = new();

        [Description("If this is not null, any user participating will get that run number's participation emblem.")]
        public int? RunNumber = null;

        [Description("If true, inputs are muted until explicitly unmuted. " +
                     "Muting also implies that no emblems are handed out and no run statistics are recorded. " +
                     "Inputs can be unmuted at runtime using !unmuteinputs or calling /start_run on the inputserver. " +
                     "Inputs can be muted at runtime using !muteinputs or calling /stop_run on the inputserver. ")]
        public bool MuteInputsAtStartup = false;
    }
}
