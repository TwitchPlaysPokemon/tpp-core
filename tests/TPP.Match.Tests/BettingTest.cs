using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using TPP.Model;

namespace TPP.Match.Tests;

public class BettingTest
{
    [TestFixture]
    private class BettingOdds
    {
        [Test]
        public void no_bets_is_no_error()
        {
            IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(0L));

            // not a useful case, but it should gracefully handle a possibly 0/0
            IImmutableDictionary<Side, double> odds = bettingShop.GetOdds();
            Assert.AreEqual(1.0d, odds[Side.Blue]);
            Assert.AreEqual(1.0d, odds[Side.Red]);
        }

        [Test]
        public async Task bets_on_only_one_side_bets_is_no_error()
        {
            IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(long.MaxValue));
            Assert.Null(await bettingShop.PlaceBet("user", Side.Blue, 1));

            // not a useful case, but it should gracefully handle a possibly x/0
            IImmutableDictionary<Side, double> odds = bettingShop.GetOdds();
            Assert.AreEqual(0.0d, odds[Side.Blue]);
            Assert.AreEqual(double.PositiveInfinity, odds[Side.Red]);
        }

        [Test]
        public async Task heavily_one_sided_bets_are_accurate()
        {
            IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(long.MaxValue));
            Assert.Null(await bettingShop.PlaceBet("userBlue", Side.Blue, 1));
            Assert.Null(await bettingShop.PlaceBet("userRedSmall", Side.Red, 1));
            Assert.Null(await bettingShop.PlaceBet("userRedBig", Side.Red, 999_999_999_999));

            IImmutableDictionary<Side, double> odds = bettingShop.GetOdds();
            Assert.AreEqual(1_000_000_000_000d, odds[Side.Blue]);
            Assert.AreEqual(0.000_000_000_001d, odds[Side.Red]);
        }

        [Test]
        public async Task sum_money_won_equals_sum_money_lost()
        {
            IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(long.MaxValue));

            var (blue1, blue2, red1, red2) = (100, 150, 180, 220);
            Assert.Null(await bettingShop.PlaceBet("userBlue1", Side.Blue, blue1));
            Assert.Null(await bettingShop.PlaceBet("userBlue2", Side.Blue, blue2));
            Assert.Null(await bettingShop.PlaceBet("userRed1", Side.Red, red1));
            Assert.Null(await bettingShop.PlaceBet("userRed2", Side.Red, red2));

            IImmutableDictionary<Side, double> odds = bettingShop.GetOdds();
            Assert.AreEqual(red1 + red2, (blue1 + blue2) * odds[Side.Blue]); // if blue won
            Assert.AreEqual(blue1 + blue2, (red1 + red2) * odds[Side.Red]); // if red won
        }
    }

    [Test]
    public async Task available_funds_self_referential()
    {
        IBettingShop<string> bettingShop = null!;
        // ReSharper disable once AccessToModifiedClosure
        bettingShop = new DefaultBettingShop<string>(
            user => Task.FromResult(100 - bettingShop.GetBetsForUser(user).Sum(kvp => kvp.Value)));

        PlaceBetFailure? failure1 = await bettingShop.PlaceBet("user", Side.Blue, 101);
        Assert.IsInstanceOf<PlaceBetFailure.InsufficientFunds>(failure1);
        Assert.AreEqual(100, (failure1 as PlaceBetFailure.InsufficientFunds)?.AvailableMoney);

        Assert.Null(await bettingShop.PlaceBet("user", Side.Blue, 99));
        // already bet amount must be incorporated into available money, since the bet gets replaced
        Assert.Null(await bettingShop.PlaceBet("user", Side.Blue, 100));

        PlaceBetFailure? failure2 = await bettingShop.PlaceBet("user", Side.Blue, 101);
        Assert.IsInstanceOf<PlaceBetFailure.InsufficientFunds>(failure2);
        Assert.AreEqual(100, (failure2 as PlaceBetFailure.InsufficientFunds)?.AvailableMoney);
    }

    [Test]
    public async Task cannot_lower_bet()
    {
        IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(long.MaxValue));

        Assert.Null(await bettingShop.PlaceBet("user", Side.Blue, 100));
        PlaceBetFailure? failure = await bettingShop.PlaceBet("user", Side.Blue, 99);
        Assert.IsInstanceOf<PlaceBetFailure.CannotLowerBet>(failure);
        Assert.AreEqual(100, (failure as PlaceBetFailure.CannotLowerBet)?.ExistingBet);
    }

    [Test]
    public async Task cannot_change_side()
    {
        IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(long.MaxValue));

        Assert.Null(await bettingShop.PlaceBet("user", Side.Blue, 100));
        PlaceBetFailure? failure = await bettingShop.PlaceBet("user", Side.Red, 200);
        Assert.IsInstanceOf<PlaceBetFailure.CannotChangeSide>(failure);
        Assert.AreEqual(Side.Blue, (failure as PlaceBetFailure.CannotChangeSide)?.SideBetOn);
    }

    [Test]
    public async Task bet_too_low_or_too_high()
    {
        IBettingShop<string> bettingShop = new DefaultBettingShop<string>(
            _ => Task.FromResult(101L), minBet: 100, maxBet: 101);

        PlaceBetFailure? failureTooLow = await bettingShop.PlaceBet("user", Side.Blue, 99);
        Assert.IsInstanceOf<PlaceBetFailure.BetTooLow>(failureTooLow);
        Assert.AreEqual(100, (failureTooLow as PlaceBetFailure.BetTooLow)?.MinBet);

        PlaceBetFailure? failureTooHigh = await bettingShop.PlaceBet("user", Side.Blue, 102);
        Assert.IsInstanceOf<PlaceBetFailure.BetTooHigh>(failureTooHigh);
        Assert.AreEqual(101, (failureTooHigh as PlaceBetFailure.BetTooHigh)?.MaxBet);

        Assert.Null(await bettingShop.PlaceBet("user", Side.Blue, 100));
        Assert.Null(await bettingShop.PlaceBet("user", Side.Blue, 101));

        IImmutableDictionary<Side, IImmutableDictionary<string, long>> bets = bettingShop.GetBets();
        Assert.AreEqual(1, bets[Side.Blue].Count);
        Assert.AreEqual(0, bets[Side.Red].Count);
        Assert.AreEqual(101, bets[Side.Blue]["user"]);
    }

    [Test]
    public void min_bet_cannot_be_greater_max_bet()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            IBettingShop<string> _ = new DefaultBettingShop<string>(
                _ => Task.FromResult(100L), minBet: 101, maxBet: 100);
        });
    }
}
