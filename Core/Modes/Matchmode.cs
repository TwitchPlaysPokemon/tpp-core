using System;
using System.Threading.Tasks;
using Core.Commands.Definitions;
using Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Modes
{
    public sealed class Matchmode : IMode, IDisposable
    {
        private readonly MatchmodeConfig _matchmodeConfig;
        private readonly ILogger<Matchmode> _logger;
        private readonly StopToken _stopToken;
        private readonly ModeBase _modeBase;

        public Matchmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, MatchmodeConfig matchmodeConfig)
        {
            _matchmodeConfig = matchmodeConfig;
            _logger = loggerFactory.CreateLogger<Matchmode>();
            _stopToken = new StopToken();
            _modeBase = new ModeBase(loggerFactory, baseConfig, _stopToken);
        }

        public async Task Run()
        {
            _logger.LogInformation("Matchmode starting");
            _modeBase.Start();
            while (!_stopToken.ShouldStop)
            {
                // TODO match main loop goes here
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
            _logger.LogInformation("Matchmode ended");
        }

        public void Dispose()
        {
            _modeBase.Dispose();
        }
    }
}
