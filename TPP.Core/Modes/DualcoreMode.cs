using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Core.Commands.Definitions;
using TPP.Core.Configuration;
using TPP.Core.Overlay;

namespace TPP.Core.Modes
{
    public sealed class DualcoreMode : IMode, IDisposable
    {
        private readonly ILogger<DualcoreMode> _logger;
        private readonly StopToken _stopToken;
        private readonly ModeBase _modeBase;
        private readonly WebsocketBroadcastServer _broadcastServer;

        public DualcoreMode(ILoggerFactory loggerFactory, BaseConfig baseConfig)
        {
            _logger = loggerFactory.CreateLogger<DualcoreMode>();
            _stopToken = new StopToken();
            Setups.Databases repos = Setups.SetUpRepositories(_logger, baseConfig);
            OverlayConnection overlayConnection;
            (_broadcastServer, overlayConnection) = Setups.SetUpOverlayServer(loggerFactory);
            _modeBase = new ModeBase(loggerFactory, repos, baseConfig, _stopToken, overlayConnection);
        }

        public async Task Run()
        {
            _logger.LogInformation("Dualcore mode starting");
            _modeBase.Start();
            Task overlayWebsocketTask = _broadcastServer.Listen();
            while (!_stopToken.ShouldStop)
            {
                // there is no sequence, just busyloop
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
            await _broadcastServer.Stop();
            await overlayWebsocketTask;
            _logger.LogInformation("Dualcore mode ended");
        }

        public void Cancel()
        {
            // there main loop is basically busylooping, so we can just tell it to stop
            _stopToken.ShouldStop = true;
        }

        public void Dispose()
        {
            _modeBase.Dispose();
        }
    }
}
