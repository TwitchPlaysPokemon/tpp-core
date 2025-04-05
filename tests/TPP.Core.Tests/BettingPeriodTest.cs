using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using TPP.Match;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests;

public class BettingPeriodTest
{
    private static IBettingShop<string> SetupTestBettingShop()
    {
        var bettingShopMock = Substitute.For<IBettingShop<string>>();
        bettingShopMock.GetOdds()
            .Returns(new Dictionary<Side, double>
            {
                [Side.Blue] = 1.25,
                [Side.Red] = 0.8
            }.ToImmutableDictionary());
        bettingShopMock.GetBets().Returns(new Dictionary<Side, IImmutableDictionary<string, long>>
        {
            [Side.Blue] = new Dictionary<string, long> { ["userBlue"] = 200 }.ToImmutableDictionary(),
            [Side.Red] = new Dictionary<string, long> { ["userRed"] = 250 }.ToImmutableDictionary(),
        }.ToImmutableDictionary());
        return bettingShopMock;
    }

    [Test]
    public async Task proper_payouts_blue_wins()
    {
        var bankMock = Substitute.For<IBank<string>>();
        List<Transaction<string>> transactions = [];
        bankMock.PerformTransactions(
            Arg.Do<IEnumerable<Transaction<string>>>(txs => transactions.AddRange(txs)),
            Arg.Any<CancellationToken>()
        ).ReturnsForAnyArgs(new List<TransactionLog>());
        IBettingPeriod<string> bettingPeriod = new BettingPeriod<string>(bankMock, SetupTestBettingShop());

        Dictionary<string, long> changesBlueWon = await bettingPeriod
            .Resolve(1234, MatchResult.Blue, CancellationToken.None);
        Assert.That(changesBlueWon.Count, Is.EqualTo(2));
        Assert.That(changesBlueWon["userBlue"], Is.EqualTo(250));
        Assert.That(changesBlueWon["userRed"], Is.EqualTo(-250));

        await bankMock.Received(1)
            .PerformTransactions(Arg.Any<IEnumerable<Transaction<string>>>(), Arg.Any<CancellationToken>());
        foreach (Transaction<string> tx in transactions)
        {
            Assert.That(tx.Type, Is.EqualTo("match"));
            Assert.That(tx.AdditionalData, Is.EqualTo(new Dictionary<string, object?> { ["match"] = 1234 }));
        }
        Assert.That(transactions.Select(tx => (tx.User, tx.Change)), Is.EquivalentTo([
            ("userBlue", 250),
            ("userRed", -250)
        ]));
    }

    [Test]
    public async Task proper_payouts_red_wins()
    {
        var bankMock = Substitute.For<IBank<string>>();
        List<Transaction<string>> transactions = [];
        bankMock.PerformTransactions(
            Arg.Do<IEnumerable<Transaction<string>>>(txs => transactions.AddRange(txs)),
            Arg.Any<CancellationToken>()
        ).ReturnsForAnyArgs(new List<TransactionLog>());
        IBettingPeriod<string> bettingPeriod = new BettingPeriod<string>(bankMock, SetupTestBettingShop());

        Dictionary<string, long> changesRedWon = await bettingPeriod
            .Resolve(1234, MatchResult.Red, CancellationToken.None);
        Assert.That(changesRedWon.Count, Is.EqualTo(2));
        Assert.That(changesRedWon["userBlue"], Is.EqualTo(-200));
        Assert.That(changesRedWon["userRed"], Is.EqualTo(200));

        await bankMock.Received(1)
            .PerformTransactions(Arg.Any<IEnumerable<Transaction<string>>>(), Arg.Any<CancellationToken>());
        foreach (Transaction<string> tx in transactions)
        {
            Assert.That(tx.Type, Is.EqualTo("match"));
            Assert.That(tx.AdditionalData, Is.EqualTo(new Dictionary<string, object?> { ["match"] = 1234 }));
        }
        Assert.That(transactions.Select(tx => (tx.User, tx.Change)), Is.EquivalentTo([
            ("userBlue", -200),
            ("userRed", 200)
        ]));
    }

    [Test]
    public async Task no_payouts_on_draw()
    {
        var bankMock = Substitute.For<IBank<string>>();
        IBettingPeriod<string> bettingPeriod = new BettingPeriod<string>(bankMock, SetupTestBettingShop());

        Dictionary<string, long> changesDraw = await bettingPeriod
            .Resolve(1234, MatchResult.Draw, CancellationToken.None);
        Assert.That(changesDraw.Count, Is.EqualTo(2));
        Assert.That(changesDraw["userBlue"], Is.EqualTo(0));
        Assert.That(changesDraw["userRed"], Is.EqualTo(0));

        await bankMock.DidNotReceive()
            .PerformTransactions(Arg.Any<IEnumerable<Transaction<string>>>(), Arg.Any<CancellationToken>());
    }
}
