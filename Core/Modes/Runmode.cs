using System;
using System.Threading.Tasks;
using Core.Commands.Definitions;
using Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Modes
{
    public sealed class Runmode : IMode, IDisposable
    {
        private readonly RunmodeConfig _runmodeConfig;
        private readonly ILogger<Runmode> _logger;
        private readonly StopToken _stopToken;
        private readonly ModeBase _modeBase;

        public Runmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, RunmodeConfig runmodeConfig)
        {
            _runmodeConfig = runmodeConfig;
            _logger = loggerFactory.CreateLogger<Runmode>();
            _stopToken = new StopToken();
            _modeBase = new ModeBase(loggerFactory, baseConfig, _stopToken);
        }

        public async Task Run()
        {
            _logger.LogInformation("Runmode starting");
            _modeBase.Start();
            while (!_stopToken.ShouldStop)
            {
                // TODO run main loop goes here
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
            _logger.LogInformation("Runmode ended");
        }

        public void Dispose()
        {
            _modeBase.Dispose();
        }
    }
}
