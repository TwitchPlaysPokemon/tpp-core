using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using TPP.Match;
using TPP.Persistence.Repos;

namespace TPP.Core.Tests
{
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
                .Resolve(1234, new MatchResult(Side.Blue), CancellationToken.None);
            Assert.AreEqual(2, changesBlueWon.Count);
            Assert.AreEqual(250, changesBlueWon["userBlue"]);
            Assert.AreEqual(-250, changesBlueWon["userRed"]);

            bankMock.Verify(b => b.PerformTransactions(Capture.In(transactions), It.IsAny<CancellationToken>()),
                Times.Once);
            foreach (Transaction<string> tx in transactions.SelectMany(list => list))
            {
                Assert.AreEqual("match", tx.Type);
                Assert.AreEqual(new Dictionary<string, object?> { ["match"] = 1234 }, tx.AdditionalData);
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
                .Resolve(1234, new MatchResult(Side.Red), CancellationToken.None);
            Assert.AreEqual(2, changesRedWon.Count);
            Assert.AreEqual(-200, changesRedWon["userBlue"]);
            Assert.AreEqual(200, changesRedWon["userRed"]);

            bankMock.Verify(b => b.PerformTransactions(Capture.In(transactions), It.IsAny<CancellationToken>()),
                Times.Once);
            foreach (Transaction<string> tx in transactions.SelectMany(list => list))
            {
                Assert.AreEqual("match", tx.Type);
                Assert.AreEqual(new Dictionary<string, object?> { ["match"] = 1234 }, tx.AdditionalData);
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
                .Resolve(1234, new MatchResult(null), CancellationToken.None);
            Assert.AreEqual(2, changesDraw.Count);
            Assert.AreEqual(0, changesDraw["userBlue"]);
            Assert.AreEqual(0, changesDraw["userRed"]);

            bankMock.Verify(
                b => b.PerformTransactions(It.IsAny<IEnumerable<Transaction<string>>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
