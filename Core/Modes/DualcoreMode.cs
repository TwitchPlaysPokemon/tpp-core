using Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Modes
{
    public class DualcoreMode : ModeBase
    {
        public DualcoreMode(ILoggerFactory loggerFactory, BaseConfig baseConfig) : base(loggerFactory, baseConfig)
        {
        }
    }
}
