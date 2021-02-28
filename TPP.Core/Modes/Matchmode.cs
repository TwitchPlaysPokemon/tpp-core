using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Commands.Definitions;
using TPP.Core.Configuration;
using TPP.Core.Overlay;
using TPP.Core.Overlay.Events;
using TPP.Match;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.Core.Modes
{
    public sealed class Matchmode : IMode, IDisposable
    {
        private readonly MatchmodeConfig _matchmodeConfig;
        private readonly ILogger<Matchmode> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly StopToken _stopToken;
        private CancellationTokenSource? _loopCancelToken;
        private readonly ModeBase _modeBase;
        private readonly WebsocketBroadcastServer _broadcastServer;
        private readonly OverlayConnection _overlayConnection;
        private readonly IBank<User> _pokeyenBank;

        public Matchmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, MatchmodeConfig matchmodeConfig)
        {
            _matchmodeConfig = matchmodeConfig;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<Matchmode>();
            _stopToken = new StopToken();
            Setups.Databases repos = Setups.SetUpRepositories(baseConfig);
            _pokeyenBank = repos.PokeyenBank;
            _modeBase = new ModeBase(loggerFactory, repos, baseConfig, _stopToken);

            _broadcastServer = new WebsocketBroadcastServer(
                loggerFactory.CreateLogger<WebsocketBroadcastServer>(), "localhost", 5001);
            _overlayConnection =
                new OverlayConnection(loggerFactory.CreateLogger<OverlayConnection>(), _broadcastServer);
        }

        public async Task Run()
        {
            _logger.LogInformation("Matchmode starting");
            _modeBase.Start();
            Task overlayWebsocketTask = _broadcastServer.Listen();
            while (!_stopToken.ShouldStop)
            {
                _loopCancelToken = new CancellationTokenSource();
                try
                {
                    await Loop(_loopCancelToken.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Match loop cancelled");
                }
            }
            await _broadcastServer.Stop();
            await overlayWebsocketTask;
            _logger.LogInformation("Matchmode ended");
        }

        private async Task Loop(CancellationToken cancellationToken)
        {
            var teams = new Teams
            {
                Blue = ImmutableList.Create(MatchTesting.TestVenonatForOverlay),
                Red = ImmutableList.Create(MatchTesting.TestVenonatForOverlay),
            };
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

            IMatchCycle match = new CoinflipMatchCycle(_loggerFactory.CreateLogger<CoinflipMatchCycle>());
            Task setupTask = match.SetUp(new MatchInfo(teams.Blue, teams.Red), cancellationToken);
            await _overlayConnection.Send(new MatchCreatedEvent(), cancellationToken);
            await _overlayConnection.Send(new MatchBettingEvent(), cancellationToken);
            await _overlayConnection.Send(new MatchModesChosenEvent(), cancellationToken); // TODO
            await _overlayConnection.Send(new MatchSettingUpEvent
            {
                MatchId = 1234,
                Teams = teams,
                BettingDuration = _matchmodeConfig.DefaultBettingDuration.TotalSeconds,
                RevealDuration = 0,
                Gimmick = "speed",
                Switching = SwitchingPolicy.Never,
                BattleStyle = BattleStyle.Singles,
                InputOptions = new InputOptions
                {
                    Moves = new MovesInputOptions
                    {
                        Policy = MoveSelectingPolicy.Always,
                        Permitted = ImmutableList.Create("a", "b", "c", "d")
                    },
                    Switches = new SwitchesInputOptions
                    {
                        Policy = SwitchingPolicy.Never,
                        Permitted = ImmutableList<string>.Empty,
                        RandomChance = 0
                    },
                    Targets = new TargetsInputOptions
                    {
                        Policy = TargetingPolicy.Disabled,
                        Permitted = ImmutableList<string>.Empty,
                        AllyHitChance = 0
                    },
                },
                BetBonus = 35,
                BetBonusType = "bet",
            }, cancellationToken);

            Duration bettingBeforeWarning = _matchmodeConfig.DefaultBettingDuration - _matchmodeConfig.WarningDuration;
            await Task.Delay(bettingBeforeWarning.ToTimeSpan(), cancellationToken);
            await _overlayConnection.Send(new MatchWarningEvent(), cancellationToken);

            await Task.Delay(_matchmodeConfig.WarningDuration.ToTimeSpan(), cancellationToken);
            await setupTask;
            Task<MatchResult> performTask = match.Perform(cancellationToken);
            await _overlayConnection.Send(new MatchPerformingEvent { Teams = teams }, cancellationToken);

            MatchResult result = await performTask;
            await _overlayConnection.Send(new MatchOverEvent { MatchResult = result }, cancellationToken);

            await Task.Delay(_matchmodeConfig.ResultDuration.ToTimeSpan(), cancellationToken);
            await _overlayConnection.Send(new ResultsFinishedEvent(), cancellationToken);
        }

        public void Cancel()
        {
            _stopToken.ShouldStop = true;
            _loopCancelToken?.Cancel();
        }

        public void Dispose()
        {
            _modeBase.Dispose();
        }
    }
}
