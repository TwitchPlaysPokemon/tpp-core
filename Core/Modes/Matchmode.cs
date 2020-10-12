using Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Modes
{
    public sealed class Matchmode : ModeBase
    {
        private readonly MatchmodeConfig _matchmodeConfig;

        public Matchmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, MatchmodeConfig matchmodeConfig)
            : base(loggerFactory, baseConfig)
        {
            _matchmodeConfig = matchmodeConfig;
        }
    }
}
