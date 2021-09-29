using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Core.Configuration;
using TPP.Core.Overlay;
using TPP.Core.Overlay.Events;
using TPP.Match;
using TPP.Model;
using TPP.Persistence;
using static TPP.Core.EventUtils;

namespace TPP.Core.Modes;

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
    private readonly IUserRepo _userRepo;
    private IBettingPeriod<User>? _bettingPeriod = null;

    public Matchmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, MatchmodeConfig matchmodeConfig)
    {
        _matchmodeConfig = matchmodeConfig;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<Matchmode>();
        _stopToken = new StopToken();
        Setups.Databases repos = Setups.SetUpRepositories(_logger, baseConfig);
        _pokeyenBank = repos.PokeyenBank;
        _userRepo = repos.UserRepo;
        (_broadcastServer, _overlayConnection) = Setups.SetUpOverlayServer(loggerFactory,
            baseConfig.OverlayWebsocketHost, baseConfig.OverlayWebsocketPort);
        _modeBase = new ModeBase(loggerFactory, repos, baseConfig, _stopToken, _overlayConnection);
        var bettingCommands = new BettingCommands(() => _bettingPeriod);
        foreach (Command command in bettingCommands.Commands)
            _modeBase.InstallAdditionalCommand(command);
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

        await ResetBalances(); //ensure everyone has money to bet before the betting period
        const int matchId = -1; // TODO
        IBettingShop<User> bettingShop = new DefaultBettingShop<User>(
            getAvailableMoney: user => _pokeyenBank.GetAvailableMoney(user));
        bettingShop.BetPlaced += (_, args) => TaskToVoidSafely(_logger, () =>
            _overlayConnection.Send(new MatchPokeyenBetUpdateEvent
            {
                MatchId = matchId,
                DefaultAction = "",
                NewBet = new Bet { Amount = args.Amount, Team = args.Side, BetBonus = 0 },
                NewBetUser = args.User,
                Odds = bettingShop.GetOdds()
            }, cancellationToken));
        _bettingPeriod = new BettingPeriod<User>(_pokeyenBank, bettingShop);
        _bettingPeriod.Start();

        IMatchCycle match = new CoinflipMatchCycle(_loggerFactory.CreateLogger<CoinflipMatchCycle>());
        Task setupTask = match.SetUp(new MatchInfo(teams.Blue, teams.Red), cancellationToken);
        await _overlayConnection.Send(new MatchCreatedEvent(), cancellationToken);
        await _overlayConnection.Send(new MatchBettingEvent(), cancellationToken);
        await _overlayConnection.Send(new MatchModesChosenEvent(), cancellationToken); // TODO
        await _overlayConnection.Send(new MatchSettingUpEvent
        {
            MatchId = matchId,
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
        _bettingPeriod.Close();
        Task<MatchResult> performTask = match.Perform(cancellationToken);
        await _overlayConnection.Send(new MatchPerformingEvent { Teams = teams }, cancellationToken);

        MatchResult result = await performTask;
        await _overlayConnection.Send(new MatchOverEvent { MatchResult = result }, cancellationToken);

        // TODO log matches
        Dictionary<User, long> changes = await _bettingPeriod.Resolve(matchId, result, cancellationToken);
        await _overlayConnection.Send(
            new MatchResultsEvent
            {
                PokeyenResults = new PokeyenResults
                {
                    Transactions = changes.ToImmutableDictionary(kvp => kvp.Key.Id,
                        kvp => new Transaction { Change = kvp.Value, NewBalance = kvp.Key.Pokeyen })
                }
            }, cancellationToken);

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

    private async Task ResetBalances()
    {
        _logger.LogDebug("Resetting Balances");
        long minimumPokeyen = _matchmodeConfig.MinimumPokeyen;
        long subscriberMinimumPokeyen = _matchmodeConfig.SubscriberMinimumPokeyen;

        List<User> poorUsers = await _userRepo.FindAllByPokeyenUnder(Math.Max(minimumPokeyen, subscriberMinimumPokeyen));
        List<Transaction<User>> transactions = new();
        foreach (User user in poorUsers)
        {
            long pokeyen = user.Pokeyen;
            if (user.IsSubscribed && pokeyen < subscriberMinimumPokeyen)
            {
                long amountToGive = subscriberMinimumPokeyen - pokeyen;
                transactions.Add(new Transaction<User>(user, amountToGive, TransactionType.Welfare));
                // TODO whisper users informing them they have been given money
                _logger.LogDebug("Subscriber {User} had their balance reset to P{Balance} (+P{BalanceDelta})",
                    user, subscriberMinimumPokeyen, amountToGive);

            }
            else if (!user.IsSubscribed && user.Pokeyen < minimumPokeyen)
            {
                long amountToGive = minimumPokeyen - pokeyen;
                transactions.Add(new Transaction<User>(user, amountToGive, TransactionType.Welfare));
                // TODO whisper users informing them they have been given money
                _logger.LogDebug("User {User} had their balance reset to P{Balance} (+P{BalanceDelta})",
                    user, minimumPokeyen, amountToGive);
            }
        }
        await _pokeyenBank.PerformTransactions(transactions);
    }
}
