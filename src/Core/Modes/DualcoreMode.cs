using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using Core.Commands.Definitions;
using Core.Configuration;
using Core.Overlay;
using Core.Utils;

namespace Core.Modes;

public sealed class DualcoreMode : IWithLifecycle
{
    private readonly ILogger<DualcoreMode> _logger;
    private readonly ModeBase _modeBase;
    private readonly WebsocketBroadcastServer _broadcastServer;
    private readonly DatabaseLock _databaseLock;

    public DualcoreMode(ILoggerFactory loggerFactory, BaseConfig baseConfig, CancellationTokenSource cancellationTokenSource)
    {
        _logger = loggerFactory.CreateLogger<DualcoreMode>();
        IStopToken stopToken = new CancellationStopToken(cancellationTokenSource);
        Setups.Databases repos = Setups.SetUpRepositories(loggerFactory, _logger, baseConfig);
        OverlayConnection overlayConnection;
        (_broadcastServer, overlayConnection) = Setups.SetUpOverlayServer(loggerFactory,
            baseConfig.OverlayWebsocketHost, baseConfig.OverlayWebsocketPort);
        _modeBase = new ModeBase(loggerFactory, repos, baseConfig, stopToken, null, overlayConnection);
        _databaseLock = new DatabaseLock(
            loggerFactory.CreateLogger<DatabaseLock>(), SystemClock.Instance, repos.KeyValueStore);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        await using IAsyncDisposable dbLock = await _databaseLock.Acquire();
        _logger.LogInformation("Dualcore mode starting");
        await TaskUtils.WhenAllFastExit(
            _modeBase.Start(cancellationToken),
            _broadcastServer.Start(cancellationToken)
        );
        _logger.LogInformation("Dualcore mode ended");
    }
}
