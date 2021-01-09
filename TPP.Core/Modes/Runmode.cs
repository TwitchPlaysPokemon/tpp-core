using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Core.Commands.Definitions;
using TPP.Core.Configuration;

namespace TPP.Core.Modes
{
    public sealed class Runmode : IMode, IDisposable
    {
        private readonly ILogger<Runmode> _logger;
        private readonly StopToken _stopToken;
        private readonly ModeBase _modeBase;

        public Runmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, Func<RunmodeConfig> configLoader)
        {
            RunmodeConfig runmodeConfig = configLoader();
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

        public void Cancel()
        {
            // once the mainloop is not just busylooping, this needs to be replaced with something
            // that makes the mode stop immediately
            _stopToken.ShouldStop = true;
        }

        public void Dispose()
        {
            _modeBase.Dispose();
        }
    }
}
