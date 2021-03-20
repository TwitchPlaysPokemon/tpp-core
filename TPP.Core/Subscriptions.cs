using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.Core
{
    /// Information on a user receiving a subscription (either by subscribing by themselves or getting a gift sub).
    public record SubscriptionInfo(
        User Subscriber,
        int NumMonths,
        int StreakMonths,
        SubscriptionTier Tier,
        string PlanName,
        Instant SubscriptionAt,
        string? Message,
        User? Gifter,
        bool IsAnonymous);

    /// <summary>
    /// Information on a user gifting a subscription to another user.
    /// The subscription itself is described as a separate <see cref="SubscriptionInfo"/> object.
    /// </summary>
    public record SubscriptionGiftInfo(
        User Gifter,
        SubscriptionTier Tier,
        bool IsAnonymous);

    public interface ISubscriptionProcessor
    {
        public record SubResult
        {
            private SubResult()
            {
                // Having a private constructor and all subtypes be sealed makes the set of all possible subtypes a
                // closed set, simulating a sum type.
            }

            public sealed record Ok(
                int DeltaTokens,
                int OldLoyaltyLeague,
                int NewLoyaltyLeague,
                int CumulativeMonths,
                User? Gifter,
                bool IsAnonymous,
                bool SubCountCorrected) : SubResult;
            public sealed record SameMonth(int Month) : SubResult;
        }

        public record SubGiftResult
        {
            private SubGiftResult()
            {
                // Having a private constructor and all subtypes be sealed makes the set of all possible subtypes a
                // closed set, simulating a sum type.
            }

            /// The gifter successfully received a token reward
            public sealed record Ok(int DeltaTokens, bool IsAnonymous) : SubGiftResult;
            /// The gifter is linked to the gift recipient and has not received a token reward
            public sealed record LinkedAccount(User LinkedUser) : SubGiftResult;
        }

        Task<SubResult> ProcessSubscription(SubscriptionInfo subscriptionInfo);
        Task<SubGiftResult> ProcessSubscriptionGift(SubscriptionGiftInfo subscriptionGiftInfo);
    }

    public class SubscriptionProcessor : ISubscriptionProcessor
    {
        private readonly IBank<User> _tokenBank;
        private readonly IUserRepo _userRepo;
        private readonly ISubscriptionLogRepo _subscriptionLogRepo;

        public SubscriptionProcessor(
            IBank<User> tokenBank, IUserRepo userRepo, ISubscriptionLogRepo subscriptionLogRepo)
        {
            _tokenBank = tokenBank;
            _userRepo = userRepo;
            _subscriptionLogRepo = subscriptionLogRepo;
        }

        public async Task<ISubscriptionProcessor.SubResult> ProcessSubscription(SubscriptionInfo subscriptionInfo)
        {
            User user = subscriptionInfo.Subscriber;
            if (user.MonthsSubscribed == subscriptionInfo.NumMonths &&
                user.SubscriptionTier?.ToRank() >= subscriptionInfo.Tier.ToRank())
            {
                // If twitch reports the new months as being the same as the old months, such as due to repeated message
                // or other error, ignore the sub but send a warning to the user in case of issues.
                return new ISubscriptionProcessor.SubResult.SameMonth(subscriptionInfo.NumMonths);
            }

            bool subCountCorrected = false;
            if (user.MonthsSubscribed > subscriptionInfo.NumMonths)
            {
                // Repair the user data and re-read it for further processing.
                // Set to current months - 1 to process as a 1-month subscription further down.
                user = await _userRepo.SetSubscriptionInfo(
                    user, subscriptionInfo.NumMonths - 1, subscriptionInfo.Tier, user.LoyaltyLeague,
                    user.SubscriptionUpdatedAt);
                subCountCorrected = true;
            }
            // If our internal months subscribed count is less than what Twitch says it is,
            // we update our count and give the subscriber "back pay" tokens.
            // This can occur when users (re)subscribe while the tpp bot is down.

            Debug.Assert(subscriptionInfo.NumMonths > user.MonthsSubscribed ||
                         (subscriptionInfo.NumMonths == user.MonthsSubscribed &&
                          subscriptionInfo.Tier.ToRank() > user.SubscriptionTier!.Value.ToRank()));

            // Difference between previous recorded subscribed months and new amount.
            int monthsDifference = subscriptionInfo.NumMonths - user.MonthsSubscribed;
            int loyaltyCompletions;
            if (monthsDifference > 0)
            {
                // Additional benefits for expensive plans are given only for the current months to prevent users from
                // tricking us into giving them extra "back pay", e.g. by purposefully not announcing lower grade
                // subscriptions from previous months.
                loyaltyCompletions = monthsDifference - 1 + subscriptionInfo.Tier.ToRank();
            }
            else
            {
                Debug.Assert(user.SubscriptionTier.HasValue,
                    "no months difference must mean it's a tier upgrade, so the user has subscribed before");
                // Same month, but an upgrade in subscription tier. Pay the difference in monetary rank.
                loyaltyCompletions = subscriptionInfo.Tier.ToRank() - user.SubscriptionTier.Value.ToRank();
            }

            const int maxLeague = 15;
            const int tokensPerLeague = 2;
            const int baseTokens = 10;
            int newLoyaltyLeague = Math.Min(user.LoyaltyLeague + loyaltyCompletions, maxLeague);
            int tokens = Enumerable
                .Range(user.LoyaltyLeague, loyaltyCompletions)
                .Select(league => baseTokens + Math.Min(league, maxLeague) * tokensPerLeague)
                .Sum();
            int oldLoyaltyLeague = user.LoyaltyLeague;

            SubscriptionLog subscriptionLog = await _subscriptionLogRepo.LogSubscription(
                user.Id, subscriptionInfo.SubscriptionAt,
                subscriptionInfo.StreakMonths, user.MonthsSubscribed, subscriptionInfo.NumMonths, monthsDifference,
                oldLoyaltyLeague, newLoyaltyLeague, loyaltyCompletions, tokens,
                subscriptionInfo.Message, subscriptionInfo.Tier, subscriptionInfo.PlanName);

            TransactionLog transactionLog = await _tokenBank.PerformTransaction(new Transaction<User>(
                user, tokens, TransactionType.Subscription, new Dictionary<string, object?>
                {
                    ["previous_months_subscribed"] = user.MonthsSubscribed,
                    ["new_months_subscribed"] = subscriptionInfo.NumMonths,
                    ["months_difference"] = monthsDifference,
                    ["previous_loyalty_tier"] = oldLoyaltyLeague,
                    ["new_loyalty_tier"] = newLoyaltyLeague,
                    ["loyalty_completions"] = loyaltyCompletions
                }));
            user = await _userRepo.SetIsSubscribed(user, true);
            user = await _userRepo.SetSubscriptionInfo(
                user, subscriptionInfo.NumMonths, subscriptionInfo.Tier, newLoyaltyLeague,
                subscriptionInfo.SubscriptionAt);
            return new ISubscriptionProcessor.SubResult.Ok(
                DeltaTokens: tokens,
                OldLoyaltyLeague: oldLoyaltyLeague,
                NewLoyaltyLeague: newLoyaltyLeague,
                CumulativeMonths: subscriptionInfo.NumMonths,
                Gifter: subscriptionInfo.Gifter,
                IsAnonymous: subscriptionInfo.IsAnonymous,
                SubCountCorrected: subCountCorrected);
        }

        public async Task<ISubscriptionProcessor.SubGiftResult> ProcessSubscriptionGift(
            SubscriptionGiftInfo subscriptionGiftInfo)
        {
            // TODO check linked accounts

            const int tokensPerRank = 10;
            int rewardTokens = subscriptionGiftInfo.Tier.ToRank() * tokensPerRank;
            TransactionLog transactionLog = await _tokenBank.PerformTransaction(new Transaction<User>(
                subscriptionGiftInfo.Gifter,
                rewardTokens,
                TransactionType.SubscriptionGift
            ));

            return new ISubscriptionProcessor.SubGiftResult.Ok(rewardTokens, subscriptionGiftInfo.IsAnonymous);
        }
    }
}
