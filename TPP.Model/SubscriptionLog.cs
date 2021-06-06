using NodaTime;
using TPP.Common;

namespace TPP.Model
{
    public record SubscriptionLog(
        string Id,
        string UserId,
        Instant Timestamp,
        int MonthsStreak,
        int MonthsNumPrev,
        int MonthsNumNew,
        int MonthsDifference,
        int LoyaltyLeaguePrev,
        int LoyaltyLeagueNew,
        int LoyaltyCompletions,
        int RewardTokens,
        string? SubMessage,
        SubscriptionTier SubPlan,
        string SubPlanName);
}
