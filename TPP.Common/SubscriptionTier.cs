using System;

namespace TPP.Common;

public enum SubscriptionTier { Prime, Tier1, Tier2, Tier3 }

public static class SubscriberTierExtensions
{
    /// The "rank" of a subscription tier is one increment per $5 worth of subscriptions. This allows the tiers
    /// to be processed numerically instead of having to deal with every possible tier individually.
    public static int ToRank(this SubscriptionTier tier) =>
        tier switch
        {
            SubscriptionTier.Prime => 1,
            SubscriptionTier.Tier1 => 1,
            SubscriptionTier.Tier2 => 2,
            SubscriptionTier.Tier3 => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "unknown subscription tier")
        };
}
