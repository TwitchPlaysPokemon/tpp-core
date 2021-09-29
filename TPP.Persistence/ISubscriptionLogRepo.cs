using TPP.Common;

namespace TPP.Persistence;

public interface ISubscriptionLogRepo
{
    Task<SubscriptionLog> LogSubscription(
        string userId,
        Instant timestamp,
        int monthsStreak,
        int monthsNumPrev,
        int monthsNumNew,
        int monthsDifference,
        int loyaltyLeaguePrev,
        int loyaltyLeagueNew,
        int loyaltyCompletions,
        int rewardTokens,
        string? subMessage,
        SubscriptionTier subPlan,
        string subPlanName);
}
