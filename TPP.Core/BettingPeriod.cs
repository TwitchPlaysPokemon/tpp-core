using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TPP.Match;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core;

public interface IBettingPeriod<T> where T : notnull
{
    /// Whether bets are currently accepted
    bool IsBettingOpen { get; }

    /// The underlying betting shop bets are placed in
    IBettingShop<T> BettingShop { get; }

    /// Start the betting period, accepting bets
    void Start();

    /// Stop the betting period, no longer accepting bets
    void Close();

    /// Resolve the bets for the supplied outcome,
    /// adjusting balances as necessary and returning what was changed.
    Task<Dictionary<T, long>> Resolve(int matchId, MatchResult result, CancellationToken cancellationToken);
}

public sealed class BettingPeriod<T> : IBettingPeriod<T> where T : notnull
{
    private readonly IBank<T> _bank;

    public bool IsBettingOpen { get; private set; } = false;
    public IBettingShop<T> BettingShop { get; }

    public BettingPeriod(IBank<T> bank, IBettingShop<T> bettingShop)
    {
        _bank = bank;
        BettingShop = bettingShop;
    }

    private Task<long> BettingChecker(T user) =>
        Task.FromResult(BettingShop.GetBetsForUser(user).Sum(kvp => kvp.Value));

    public void Start()
    {
        _bank.AddReservedMoneyChecker(BettingChecker);
        IsBettingOpen = true;
    }

    public void Close()
    {
        IsBettingOpen = false;
    }

    public async Task<Dictionary<T, long>> Resolve(
        int matchId, MatchResult result, CancellationToken cancellationToken)
    {
        IImmutableDictionary<Side, IImmutableDictionary<T, long>> bets = BettingShop.GetBets();
        IImmutableDictionary<Side, double> odds = BettingShop.GetOdds();

        Dictionary<T, long> changes = new();
        if (result == MatchResult.Draw)
        {
            foreach (T user in bets[Side.Blue].Keys.Union(bets[Side.Red].Keys))
                changes[user] = 0;
        }
        else
        {
            Side winner = result.ToSide()!.Value;
            Side loser = winner == Side.Blue ? Side.Red : Side.Blue;
            foreach ((T user, long bet) in bets[winner])
                changes[user] = Math.Max(1, (int)Math.Ceiling(bet * odds[winner]));
            foreach ((T user, long bet) in bets[loser])
                changes[user] = -bet;
        }
        List<Transaction<T>> transactions = changes
            .Where(kvp => kvp.Value != 0)
            .Select(kvp => new Transaction<T>(
                kvp.Key,
                kvp.Value,
                TransactionType.Match,
                new Dictionary<string, object?> { ["match"] = matchId }
            ))
            .ToList();
        if (transactions.Count > 0)
            await _bank.PerformTransactions(transactions, cancellationToken);
        _bank.RemoveReservedMoneyChecker(BettingChecker);

        return changes;
    }
}
