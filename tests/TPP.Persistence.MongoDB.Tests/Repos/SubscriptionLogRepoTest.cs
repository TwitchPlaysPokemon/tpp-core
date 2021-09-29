using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NodaTime;
using NUnit.Framework;
using TPP.Common;
using TPP.Model;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos;

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
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.MonthsStreak, Is.EqualTo(monthsStreak));
        Assert.That(written.MonthsNumPrev, Is.EqualTo(monthsNumPrev));
        Assert.That(written.MonthsNumNew, Is.EqualTo(monthsNumNew));
        Assert.That(written.MonthsDifference, Is.EqualTo(monthsDifference));
        Assert.That(written.LoyaltyLeaguePrev, Is.EqualTo(loyaltyLeaguePrev));
        Assert.That(written.LoyaltyLeagueNew, Is.EqualTo(loyaltyLeagueNew));
        Assert.That(written.LoyaltyCompletions, Is.EqualTo(loyaltyCompletions));
        Assert.That(written.RewardTokens, Is.EqualTo(rewardTokens));
        Assert.That(written.SubMessage, Is.EqualTo(subMessage));
        Assert.That(written.SubPlan, Is.EqualTo(subPlan));
        Assert.That(written.SubPlanName, Is.EqualTo(subPlanName));
        Assert.NotNull(written.Id);

        // read from db
        List<SubscriptionLog> allItems = await repo.Collection.Find(FilterDefinition<SubscriptionLog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        SubscriptionLog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));

        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.MonthsStreak, Is.EqualTo(monthsStreak));
        Assert.That(read.MonthsNumPrev, Is.EqualTo(monthsNumPrev));
        Assert.That(read.MonthsNumNew, Is.EqualTo(monthsNumNew));
        Assert.That(read.MonthsDifference, Is.EqualTo(monthsDifference));
        Assert.That(read.LoyaltyLeaguePrev, Is.EqualTo(loyaltyLeaguePrev));
        Assert.That(read.LoyaltyLeagueNew, Is.EqualTo(loyaltyLeagueNew));
        Assert.That(read.LoyaltyCompletions, Is.EqualTo(loyaltyCompletions));
        Assert.That(read.RewardTokens, Is.EqualTo(rewardTokens));
        Assert.That(read.SubMessage, Is.EqualTo(subMessage));
        Assert.That(read.SubPlan, Is.EqualTo(subPlan));
        Assert.That(read.SubPlanName, Is.EqualTo(subPlanName));
    }
}
