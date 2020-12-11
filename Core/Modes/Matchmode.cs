using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Core.Commands.Definitions;
using Core.Configuration;
using Core.Overlay;
using Core.Overlay.Events;
using Match;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Core.Modes
{
    public sealed class Matchmode : IMode, IDisposable
    {
        private readonly MatchmodeConfig _matchmodeConfig;
        private readonly ILogger<Matchmode> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly StopToken _stopToken;
        private readonly ModeBase _modeBase;
        private readonly WebsocketBroadcastServer _broadcastServer;
        private readonly OverlayConnection _overlayConnection;

        public Matchmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, MatchmodeConfig matchmodeConfig)
        {
            _matchmodeConfig = matchmodeConfig;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<Matchmode>();
            _stopToken = new StopToken();
            _modeBase = new ModeBase(loggerFactory, baseConfig, _stopToken);

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
                await Loop();
            }
            await _broadcastServer.Stop();
            await overlayWebsocketTask;
            _logger.LogInformation("Matchmode ended");
        }

        private async Task Loop()
        {
            var teams = new Teams
            {
                Blue = ImmutableList.Create(MatchTesting.TestVenonatForOverlay),
                Red = ImmutableList.Create(MatchTesting.TestVenonatForOverlay),
            };
            await Task.Delay(TimeSpan.FromSeconds(3));

            IMatchCycle match = new CoinflipMatchCycle(_loggerFactory.CreateLogger<CoinflipMatchCycle>());
            Task setupTask = match.SetUp(new MatchInfo(teams.Blue, teams.Red));
            await _overlayConnection.Send(new MatchCreatedEvent());
            await _overlayConnection.Send(new MatchBettingEvent());
            await _overlayConnection.Send(new MatchModesChosenEvent()); // TODO
            await _overlayConnection.Send(new MatchSettingUpEvent
            {
                MatchId = 1234,
                Teams = teams,
                BettingDuration = _matchmodeConfig.DefaultBettingDuration.TotalSeconds,
                RevealDuration = 0,
                Gimmick = "speed",
                Switching = "never",
                BattleStyle = "singles",
                InputOptions = new InputOptions
                {
                    Moves = new MovesInputOptions
                    {
                        Policy = "always",
                        Permitted = ImmutableList.Create("a", "b", "c", "d")
                    },
                    Switches = new SwitchesInputOptions
                    {
                        Policy = "never",
                        Permitted = ImmutableList<string>.Empty,
                        RandomChance = 0
                    },
                    Targets = new TargetsInputOptions
                    {
                        Policy = "disabled",
                        Permitted = ImmutableList<string>.Empty,
                        AllyHitChance = 0
                    },
                },
                BetBonus = 35,
                BetBonusType = "bet",
            });

            Duration bettingBeforeWarning = _matchmodeConfig.DefaultBettingDuration - _matchmodeConfig.WarningDuration;
            await Task.Delay(bettingBeforeWarning.ToTimeSpan());
            await _overlayConnection.Send(new MatchWarningEvent());

            await Task.Delay(_matchmodeConfig.WarningDuration.ToTimeSpan());
            await setupTask;
            Task<MatchResult> performTask = match.Perform();
            await _overlayConnection.Send(new MatchPerformingEvent { Teams = teams });

            MatchResult result = await performTask;
            object winnerForOverlay = result.Winner switch { Side.Blue => 0, Side.Red => 1, _ => "draw" };
            await _overlayConnection.Send(new MatchOverEvent { MatchResult = winnerForOverlay });

            await Task.Delay(_matchmodeConfig.ResultDuration.ToTimeSpan());
            await _overlayConnection.Send(new ResultsFinishedEvent());
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
