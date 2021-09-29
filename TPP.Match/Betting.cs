using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Model;

namespace TPP.Match;

/// Failure outcome of attempting to place a bet.
/// This is an exhaustive set of possible failures,
/// so consumers of this type can handle them all without knowledge about the implementation.
/// However, implementations might not use all available responses if they don't need to.
public record PlaceBetFailure
{
    private PlaceBetFailure()
    {
        // private constructor with all subclasses being sealed and within this class
        // makes the set of subtypes a closed set, allowing us to simulate a sum type.
    }

    /// User already made a higher bet, and bets cannot be lowered.
    public sealed record CannotLowerBet(long ExistingBet) : PlaceBetFailure;
    /// User already made a bet on a different side, and users can only bet on one side.
    public sealed record CannotChangeSide(Side SideBetOn) : PlaceBetFailure;
    /// User does not have enough money for that bet.
    public sealed record InsufficientFunds(long AvailableMoney) : PlaceBetFailure;
    /// The current betting policy requires the user to bet more.
    public sealed record BetTooLow(long MinBet) : PlaceBetFailure;
    /// The current betting policy requires the user to bet less.
    public sealed record BetTooHigh(long MaxBet) : PlaceBetFailure;
}

public class BetPlacedEventArgs<TUser> : EventArgs
{
    public TUser User { get; }
    public Side Side { get; }
    public long Amount { get; }

    public BetPlacedEventArgs(TUser user, Side side, long amount)
    {
        User = user;
        Side = side;
        Amount = amount;
    }
}

/// <summary>
/// A betting shop.
/// Users can place bets, the bets can be retrieved afterwards, and odds for each side are calculated.
/// </summary>
/// <typeparam name="TUser">User-Type. Typically just "User", but to ease testing and not require a dependency
/// on the persistence User class, this is generic.</typeparam>
public interface IBettingShop<TUser> where TUser : notnull
{
    /// Attempts to place a bet for a user.
    /// If placing the bet was successful, null is returned.
    /// Otherwise this can fail for various reasons, which are described in this method's return value.
    public Task<PlaceBetFailure?> PlaceBet(TUser user, Side side, long amount);

    /// When a user successfully places a bet.
    /// This may also be a replacement of that user's previous bet.
    public event EventHandler<BetPlacedEventArgs<TUser>> BetPlaced;

    /// Calculates and returns the odds per side,
    /// as a floating point number describing how much payout that side would get if they won.
    /// For example, for a 2-sided match between side "blue" and "red" with twice as much money on blue
    /// the odds would be 0.5 for blue and 2.0 for red.
    public IImmutableDictionary<Side, double> GetOdds();

    /// Returns the bets per side and per user.
    /// The outer dictionary will be populated with all sides, but the inner dictionary may be empty.
    public IImmutableDictionary<Side, IImmutableDictionary<TUser, long>> GetBets();

    /// Returns the bets per side for a specific user.
    /// The dictionary will be populated with all sides, but the values may be zero.
    public IImmutableDictionary<Side, long> GetBetsForUser(TUser user);
}

public class DefaultBettingShop<TUser> : IBettingShop<TUser> where TUser : notnull
{
    private readonly Func<TUser, Task<long>> _getAvailableMoney;
    private readonly long _minBet;
    private readonly long _maxBet;

    public event EventHandler<BetPlacedEventArgs<TUser>>? BetPlaced;

    private readonly Dictionary<Side, Dictionary<TUser, long>> _bets = Enum
        .GetValues(typeof(Side))
        .Cast<Side>()
        .ToDictionary(t => t, _ => new Dictionary<TUser, long>());

    /// <summary>
    /// A default implementation of <see cref="IBettingShop{TUser}"/> without any special features.
    /// </summary>
    /// <param name="getAvailableMoney">Function determining how much money a user can bet. It is important that
    /// this function deducts any amount already bet, possibly using <see cref="GetBetsForUser"/> for that.</param>
    /// <param name="minBet">Minimum amount of money that can be bet.</param>
    /// <param name="maxBet">Maximum amount of money that can be bet.</param>
    public DefaultBettingShop(Func<TUser, Task<long>> getAvailableMoney, long minBet = 1, long maxBet = long.MaxValue)
    {
        _getAvailableMoney = getAvailableMoney;
        if (minBet > maxBet)
            throw new ArgumentException($"{nameof(minBet)} cannot be greater than {nameof(maxBet)}");
        _minBet = minBet;
        _maxBet = maxBet;
    }

    public async Task<PlaceBetFailure?> PlaceBet(TUser user, Side side, long amount)
    {
        if (amount < _minBet)
            return new PlaceBetFailure.BetTooLow(_minBet);
        if (amount > _maxBet)
            return new PlaceBetFailure.BetTooHigh(_maxBet);
        foreach ((Side loopSide, Dictionary<TUser, long> betsForSide) in _bets)
        {
            if (Equals(loopSide, side))
            {
                long existingBet = betsForSide.GetValueOrDefault(user, 0);
                if (existingBet > amount)
                    return new PlaceBetFailure.CannotLowerBet(existingBet);
            }
            else
            {
                if (betsForSide.ContainsKey(user))
                    return new PlaceBetFailure.CannotChangeSide(loopSide);
            }
        }
        long currentBet = _bets[side].GetValueOrDefault(user, 0);
        long availableForBet = await _getAvailableMoney(user) + currentBet;
        if (amount > availableForBet)
            return new PlaceBetFailure.InsufficientFunds(availableForBet);
        _bets[side][user] = amount;
        BetPlaced?.Invoke(this, new BetPlacedEventArgs<TUser>(user, side, amount));
        return null;
    }

    public IImmutableDictionary<Side, double> GetOdds()
    {
        Dictionary<Side, long> betSums = _bets.ToDictionary(
            sideKvp => sideKvp.Key,
            sideKvp => sideKvp.Value.Sum(userKvp => userKvp.Value));
        return betSums.ToImmutableDictionary(
            selfKvp => selfKvp.Key,
            selfKvp =>
            {
                long sumOthers = betSums
                    .Where(othersKvp => !Equals(othersKvp.Key, selfKvp.Key))
                    .Sum(othersKvp => othersKvp.Value);
                return selfKvp.Value == 0
                    ? sumOthers == 0 ? 1 : double.PositiveInfinity
                    : sumOthers / (double)selfKvp.Value;
            });
    }

    public IImmutableDictionary<Side, IImmutableDictionary<TUser, long>> GetBets() =>
        _bets.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => (IImmutableDictionary<TUser, long>)kvp.Value.ToImmutableDictionary());

    public IImmutableDictionary<Side, long> GetBetsForUser(TUser user) =>
        _bets.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.GetValueOrDefault(user, 0));
}
