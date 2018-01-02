using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace TPPCore.Service.Common
{
    public class ServiceRunnerOptions
    {
        [Option('r', "restart-on-error", Default = false,
            HelpText = "Whether the service should be restarted on an error")]
        public bool RestartOnError { get; set; }

        [Option('l', "log-config",
            HelpText = "Path of the log4net logging configuration file")]
        public string LogConfig { get; set; }

        [Option('c', "config",
            HelpText = "Path of configuration file")]
        public IEnumerable<string> ConfigFiles { get; set; }
    }
}
