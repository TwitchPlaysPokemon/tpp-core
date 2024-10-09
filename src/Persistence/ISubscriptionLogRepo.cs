using System.Collections.Generic;
using System.Threading.Tasks;
using NodaTime;
using TPP.Common;
using TPP.Model;

namespace TPP.Persistence;

public interface ISubscriptionLogRepo
{
    Task<SubscriptionLog> LogSubscription(
        string userId,
        Instant timestamp,
        int? monthsStreak,
        int monthsNumPrev,
        int monthsNumNew,
        int monthsDifference,
        int loyaltyLeaguePrev,
        int loyaltyLeagueNew,
        int loyaltyCompletions,
        int rewardTokens,
        bool isGift,
        string? subMessage,
        SubscriptionTier subPlan,
        string? subPlanName);

    /// Returns recipient user IDs of all recent gift subs whose timestamp is no older than the supplied cutoff.
    Task<List<string>> FindRecentGiftSubs(Instant cutoff);
}
