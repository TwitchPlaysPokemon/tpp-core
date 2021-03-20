using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NodaTime;
using NUnit.Framework;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos
{
    public class SubscriptionLogRepoTest : MongoTestBase
    {
        [Test]
        public async Task persists_successfully()
        {
            SubscriptionLogRepo repo = new(CreateTemporaryDatabase());
            Instant timestamp = Instant.FromUnixTimeSeconds(123);
            const string userId = "123";
            const int monthsStreak = 101;
            const int monthsNumPrev = 103;
            const int monthsNumNew = 105;
            const int monthsDifference = 2;
            const int loyaltyLeaguePrev = 20;
            const int loyaltyLeagueNew = 21;
            const int loyaltyCompletions = 1;
            const int rewardTokens = 10;
            const string subMessage = "message text";
            const SubscriptionTier subPlan = SubscriptionTier.Tier2;
            const string subPlanName = "plan name";

            // persist to db
            SubscriptionLog written = await repo.LogSubscription(userId, timestamp,
                monthsStreak, monthsNumPrev, monthsNumNew, monthsDifference,
                loyaltyLeaguePrev, loyaltyLeagueNew, loyaltyCompletions, rewardTokens,
                subMessage, subPlan, subPlanName);
            Assert.AreEqual(timestamp, written.Timestamp);
            Assert.AreEqual(userId, written.UserId);
            Assert.AreEqual(monthsStreak, written.MonthsStreak);
            Assert.AreEqual(monthsNumPrev, written.MonthsNumPrev);
            Assert.AreEqual(monthsNumNew, written.MonthsNumNew);
            Assert.AreEqual(monthsDifference, written.MonthsDifference);
            Assert.AreEqual(loyaltyLeaguePrev, written.LoyaltyLeaguePrev);
            Assert.AreEqual(loyaltyLeagueNew, written.LoyaltyLeagueNew);
            Assert.AreEqual(loyaltyCompletions, written.LoyaltyCompletions);
            Assert.AreEqual(rewardTokens, written.RewardTokens);
            Assert.AreEqual(subMessage, written.SubMessage);
            Assert.AreEqual(subPlan, written.SubPlan);
            Assert.AreEqual(subPlanName, written.SubPlanName);
            Assert.NotNull(written.Id);

            // read from db
            List<SubscriptionLog> allItems = await repo.Collection.Find(FilterDefinition<SubscriptionLog>.Empty).ToListAsync();
            Assert.AreEqual(1, allItems.Count);
            SubscriptionLog read = allItems[0];
            Assert.AreEqual(written, read);

            Assert.AreEqual(timestamp, read.Timestamp);
            Assert.AreEqual(userId, read.UserId);
            Assert.AreEqual(monthsStreak, read.MonthsStreak);
            Assert.AreEqual(monthsNumPrev, read.MonthsNumPrev);
            Assert.AreEqual(monthsNumNew, read.MonthsNumNew);
            Assert.AreEqual(monthsDifference, read.MonthsDifference);
            Assert.AreEqual(loyaltyLeaguePrev, read.LoyaltyLeaguePrev);
            Assert.AreEqual(loyaltyLeagueNew, read.LoyaltyLeagueNew);
            Assert.AreEqual(loyaltyCompletions, read.LoyaltyCompletions);
            Assert.AreEqual(rewardTokens, read.RewardTokens);
            Assert.AreEqual(subMessage, read.SubMessage);
            Assert.AreEqual(subPlan, read.SubPlan);
            Assert.AreEqual(subPlanName, read.SubPlanName);
        }
    }
}
