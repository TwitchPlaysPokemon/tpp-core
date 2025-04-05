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
            Assert.That(odds[Side.Blue], Is.EqualTo(1.0d));
            Assert.That(odds[Side.Red], Is.EqualTo(1.0d));
        }

        [Test]
        public async Task bets_on_only_one_side_bets_is_no_error()
        {
            IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(long.MaxValue));
            Assert.That(await bettingShop.PlaceBet("user", Side.Blue, 1), Is.Null);

            // not a useful case, but it should gracefully handle a possibly x/0
            IImmutableDictionary<Side, double> odds = bettingShop.GetOdds();
            Assert.That(odds[Side.Blue], Is.EqualTo(0.0d));
            Assert.That(odds[Side.Red], Is.EqualTo(double.PositiveInfinity));
        }

        [Test]
        public async Task heavily_one_sided_bets_are_accurate()
        {
            IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(long.MaxValue));
            Assert.That(await bettingShop.PlaceBet("userBlue", Side.Blue, 1), Is.Null);
            Assert.That(await bettingShop.PlaceBet("userRedSmall", Side.Red, 1), Is.Null);
            Assert.That(await bettingShop.PlaceBet("userRedBig", Side.Red, 999_999_999_999), Is.Null);

            IImmutableDictionary<Side, double> odds = bettingShop.GetOdds();
            Assert.That(odds[Side.Blue], Is.EqualTo(1_000_000_000_000d));
            Assert.That(odds[Side.Red], Is.EqualTo(0.000_000_000_001d));
        }

        [Test]
        public async Task sum_money_won_equals_sum_money_lost()
        {
            IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(long.MaxValue));

            var (blue1, blue2, red1, red2) = (100, 150, 180, 220);
            Assert.That(await bettingShop.PlaceBet("userBlue1", Side.Blue, blue1), Is.Null);
            Assert.That(await bettingShop.PlaceBet("userBlue2", Side.Blue, blue2), Is.Null);
            Assert.That(await bettingShop.PlaceBet("userRed1", Side.Red, red1), Is.Null);
            Assert.That(await bettingShop.PlaceBet("userRed2", Side.Red, red2), Is.Null);

            IImmutableDictionary<Side, double> odds = bettingShop.GetOdds();
            Assert.That(red1 + red2, Is.EqualTo((blue1 + blue2) * odds[Side.Blue])); // if blue won
            Assert.That(blue1 + blue2, Is.EqualTo((red1 + red2) * odds[Side.Red])); // if red won
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
        Assert.That(failure1, Is.InstanceOf<PlaceBetFailure.InsufficientFunds>());
        Assert.That((failure1 as PlaceBetFailure.InsufficientFunds)?.AvailableMoney, Is.EqualTo(100));

        Assert.That(await bettingShop.PlaceBet("user", Side.Blue, 99), Is.Null);
        // already bet amount must be incorporated into available money, since the bet gets replaced
        Assert.That(await bettingShop.PlaceBet("user", Side.Blue, 100), Is.Null);

        PlaceBetFailure? failure2 = await bettingShop.PlaceBet("user", Side.Blue, 101);
        Assert.That(failure2, Is.InstanceOf<PlaceBetFailure.InsufficientFunds>());
        Assert.That((failure2 as PlaceBetFailure.InsufficientFunds)?.AvailableMoney, Is.EqualTo(100));
    }

    [Test]
    public async Task cannot_lower_bet()
    {
        IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(long.MaxValue));

        Assert.That(await bettingShop.PlaceBet("user", Side.Blue, 100), Is.Null);
        PlaceBetFailure? failure = await bettingShop.PlaceBet("user", Side.Blue, 99);
        Assert.That(failure, Is.InstanceOf<PlaceBetFailure.CannotLowerBet>());
        Assert.That((failure as PlaceBetFailure.CannotLowerBet)?.ExistingBet, Is.EqualTo(100));
    }

    [Test]
    public async Task cannot_change_side()
    {
        IBettingShop<string> bettingShop = new DefaultBettingShop<string>(_ => Task.FromResult(long.MaxValue));

        Assert.That(await bettingShop.PlaceBet("user", Side.Blue, 100), Is.Null);
        PlaceBetFailure? failure = await bettingShop.PlaceBet("user", Side.Red, 200);
        Assert.That(failure, Is.InstanceOf<PlaceBetFailure.CannotChangeSide>());
        Assert.That((failure as PlaceBetFailure.CannotChangeSide)?.SideBetOn, Is.EqualTo(Side.Blue));
    }

    [Test]
    public async Task bet_too_low_or_too_high()
    {
        IBettingShop<string> bettingShop = new DefaultBettingShop<string>(
            _ => Task.FromResult(101L), minBet: 100, maxBet: 101);

        PlaceBetFailure? failureTooLow = await bettingShop.PlaceBet("user", Side.Blue, 99);
        Assert.That(failureTooLow, Is.InstanceOf<PlaceBetFailure.BetTooLow>());
        Assert.That((failureTooLow as PlaceBetFailure.BetTooLow)?.MinBet, Is.EqualTo(100));

        PlaceBetFailure? failureTooHigh = await bettingShop.PlaceBet("user", Side.Blue, 102);
        Assert.That(failureTooHigh, Is.InstanceOf<PlaceBetFailure.BetTooHigh>());
        Assert.That((failureTooHigh as PlaceBetFailure.BetTooHigh)?.MaxBet, Is.EqualTo(101));

        Assert.That(await bettingShop.PlaceBet("user", Side.Blue, 100), Is.Null);
        Assert.That(await bettingShop.PlaceBet("user", Side.Blue, 101), Is.Null);

        IImmutableDictionary<Side, IImmutableDictionary<string, long>> bets = bettingShop.GetBets();
        Assert.That(bets[Side.Blue].Count, Is.EqualTo(1));
        Assert.That(bets[Side.Red].Count, Is.EqualTo(0));
        Assert.That(bets[Side.Blue]["user"], Is.EqualTo(101));
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
