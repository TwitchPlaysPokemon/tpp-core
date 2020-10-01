using Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Core
{
    public sealed class Runmode : ModeBase
    {
        private readonly RunmodeConfig _runmodeConfig;

        public Runmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, RunmodeConfig runmodeConfig)
            : base(loggerFactory, baseConfig)
        {
            _runmodeConfig = runmodeConfig;
        }
    }
}
