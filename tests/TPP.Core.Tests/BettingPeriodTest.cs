using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using TPP.Match;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests;

public class BettingPeriodTest
{
    private static IBettingShop<string> SetupTestBettingShop()
    {
        Mock<IBettingShop<string>> bettingShopMock = new();
        bettingShopMock.Setup(b => b.GetOdds())
            .Returns(new Dictionary<Side, double>
            {
                [Side.Blue] = 1.25,
                [Side.Red] = 0.8
            }.ToImmutableDictionary());
        bettingShopMock.Setup(b => b.GetBets()).Returns(new Dictionary<Side, IImmutableDictionary<string, long>>
        {
            [Side.Blue] = new Dictionary<string, long> { ["userBlue"] = 200 }.ToImmutableDictionary(),
            [Side.Red] = new Dictionary<string, long> { ["userRed"] = 250 }.ToImmutableDictionary(),
        }.ToImmutableDictionary());
        return bettingShopMock.Object;
    }

    [Test]
    public async Task proper_payouts_blue_wins()
    {
        Mock<IBank<string>> bankMock = new();
        List<IEnumerable<Transaction<string>>> transactions = new();
        IBettingPeriod<string> bettingPeriod = new BettingPeriod<string>(bankMock.Object, SetupTestBettingShop());

        Dictionary<string, long> changesBlueWon = await bettingPeriod
            .Resolve(1234, MatchResult.Blue, CancellationToken.None);
        Assert.That(changesBlueWon.Count, Is.EqualTo(2));
        Assert.That(changesBlueWon["userBlue"], Is.EqualTo(250));
        Assert.That(changesBlueWon["userRed"], Is.EqualTo(-250));

        bankMock.Verify(b => b.PerformTransactions(Capture.In(transactions), It.IsAny<CancellationToken>()),
            Times.Once);
        foreach (Transaction<string> tx in transactions.SelectMany(list => list))
        {
            Assert.That(tx.Type, Is.EqualTo("match"));
            Assert.That(tx.AdditionalData, Is.EqualTo(new Dictionary<string, object?> { ["match"] = 1234 }));
        }
        CollectionAssert.AreEquivalent(new[]
        {
            ("userBlue", 250),
            ("userRed", -250)
        }, transactions.SelectMany(list => list).Select(tx => (tx.User, tx.Change)));
    }

    [Test]
    public async Task proper_payouts_red_wins()
    {
        Mock<IBank<string>> bankMock = new();
        List<IEnumerable<Transaction<string>>> transactions = new();
        IBettingPeriod<string> bettingPeriod = new BettingPeriod<string>(bankMock.Object, SetupTestBettingShop());

        Dictionary<string, long> changesRedWon = await bettingPeriod
            .Resolve(1234, MatchResult.Red, CancellationToken.None);
        Assert.That(changesRedWon.Count, Is.EqualTo(2));
        Assert.That(changesRedWon["userBlue"], Is.EqualTo(-200));
        Assert.That(changesRedWon["userRed"], Is.EqualTo(200));

        bankMock.Verify(b => b.PerformTransactions(Capture.In(transactions), It.IsAny<CancellationToken>()),
            Times.Once);
        foreach (Transaction<string> tx in transactions.SelectMany(list => list))
        {
            Assert.That(tx.Type, Is.EqualTo("match"));
            Assert.That(tx.AdditionalData, Is.EqualTo(new Dictionary<string, object?> { ["match"] = 1234 }));
        }
        CollectionAssert.AreEquivalent(new[]
        {
            ("userBlue", -200),
            ("userRed", 200),
        }, transactions.SelectMany(list => list).Select(tx => (tx.User, tx.Change)));
    }

    [Test]
    public async Task no_payouts_on_draw()
    {
        Mock<IBank<string>> bankMock = new();
        IBettingPeriod<string> bettingPeriod = new BettingPeriod<string>(bankMock.Object, SetupTestBettingShop());

        Dictionary<string, long> changesDraw = await bettingPeriod
            .Resolve(1234, MatchResult.Draw, CancellationToken.None);
        Assert.That(changesDraw.Count, Is.EqualTo(2));
        Assert.That(changesDraw["userBlue"], Is.EqualTo(0));
        Assert.That(changesDraw["userRed"], Is.EqualTo(0));

        bankMock.Verify(
            b => b.PerformTransactions(It.IsAny<IEnumerable<Transaction<string>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
