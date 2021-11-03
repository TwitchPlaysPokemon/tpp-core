using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using TPP.Common;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos
{
    public class SubscriptionLogRepo : ISubscriptionLogRepo
    {
        public const string CollectionName = "subscriptionlog";

        public readonly IMongoCollection<SubscriptionLog> Collection;

        static SubscriptionLogRepo()
        {
            BsonClassMap.RegisterClassMap<SubscriptionLog>(cm =>
            {
                cm.MapIdProperty(i => i.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(ObjectIdAsStringSerializer.Instance);
                cm.MapProperty(i => i.UserId).SetElementName("user_id");
                cm.MapProperty(i => i.Timestamp).SetElementName("timestamp");
                cm.MapProperty(i => i.MonthsStreak).SetElementName("months_in_a_row");
                cm.MapProperty(i => i.MonthsNumPrev).SetElementName("previous_months_subscribed");
                cm.MapProperty(i => i.MonthsNumNew).SetElementName("new_months_subscribed");
                cm.MapProperty(i => i.MonthsDifference).SetElementName("difference");
                cm.MapProperty(i => i.LoyaltyLeaguePrev).SetElementName("previous_loyalty_tier");
                cm.MapProperty(i => i.LoyaltyLeagueNew).SetElementName("new_loyalty_tier");
                cm.MapProperty(i => i.LoyaltyCompletions).SetElementName("loyalty_completions");
                cm.MapProperty(i => i.RewardTokens).SetElementName("tokens");
                cm.MapProperty(i => i.SubMessage).SetElementName("message");
                cm.MapProperty(i => i.SubPlan).SetElementName("plan");
                cm.MapProperty(i => i.SubPlanName).SetElementName("plan_name");
            });
        }

        public SubscriptionLogRepo(IMongoDatabase database)
        {
            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<SubscriptionLog>(CollectionName);
        }

        public async Task<SubscriptionLog> LogSubscription(
            string userId, Instant timestamp, int monthsStreak, int monthsNumPrev,
            int monthsNumNew, int monthsDifference, int loyaltyLeaguePrev, int loyaltyLeagueNew, int loyaltyCompletions,
            int rewardTokens, string? subMessage, SubscriptionTier subPlan, string subPlanName)
        {
            var item = new SubscriptionLog(
                string.Empty, userId, timestamp,
                monthsStreak, monthsNumPrev, monthsNumNew, monthsDifference,
                loyaltyLeaguePrev, loyaltyLeagueNew, loyaltyCompletions, rewardTokens,
                subMessage, subPlan, subPlanName);
            await Collection.InsertOneAsync(item);
            return item;
        }
    }
}
