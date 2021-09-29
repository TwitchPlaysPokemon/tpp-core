using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Core.Commands.Definitions;
using TPP.Core.Configuration;

namespace TPP.Core.Modes;

/// A mode for testing purposes that can be run without any preconditions or configurations.
public sealed class DummyMode : IMode, IDisposable
{
    private readonly ILogger<DummyMode> _logger;
    private readonly StopToken _stopToken;

    public DummyMode(ILoggerFactory loggerFactory, BaseConfig baseConfig)
    {
        _logger = loggerFactory.CreateLogger<DummyMode>();
        _stopToken = new StopToken();
    }

    public async Task Run()
    {
        _logger.LogInformation("Dummy mode starting");
        while (!_stopToken.ShouldStop)
        {
            // there is no sequence, just busyloop
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
        _logger.LogInformation("Dummy mode ended");
    }

    public void Cancel()
    {
        // there main loop is basically busylooping, so we can just tell it to stop
        _stopToken.ShouldStop = true;
    }

    public void Dispose()
    {
    }
}
