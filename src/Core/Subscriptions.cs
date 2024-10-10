using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using Common;
using Model;
using Persistence;

namespace Core
{
    public record EmoteOccurrence(string Id, string Code, int StartIndex, int EndIndex);

    /// <summary>
    /// Information on a user subscription directly (not via a gift).
    /// Gifted subscriptions are using <see cref="SubscriptionGiftInfo"/> instead.
    /// </summary>
    public record SubscriptionInfo(
        User Subscriber,
        int NumMonths,
        int? StreakMonths,
        SubscriptionTier Tier,
        string? PlanName, // informational, IRC used to send something like "Channel\sSubscription:\s$24.99\sSub" along
        Instant SubscriptionAt,
        bool IsGift,
        string? Message,
        IImmutableList<EmoteOccurrence> Emotes);

    /// Information on a user subscribing through a gifted subscription.
    public record SubscriptionGiftInfo(
        User Gifter,
        SubscriptionTier Tier,
        Instant SubscriptionGiftAt,
        // int NumGiftedMonths, // Looks like Twitch no longer offers gifting multiple months at once
        int NumGifts);

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
            public sealed record Ok(int GifterTokens, int NumGifts) : SubGiftResult;

            /// Some or all tokens were witheld because the gifter is linked to some or all of the gift recipients
            public sealed record OkButLinked(int GifterTokens, int NumGifts, IImmutableList<User> LinkedUsers) : SubGiftResult;
        }

        Task<SubResult> ProcessSubscription(SubscriptionInfo subscriptionInfo);
        Task<SubGiftResult> ProcessSubscriptionGift(SubscriptionGiftInfo subGiftInfo);
    }

    public class SubscriptionProcessor(
        ILogger<SubscriptionProcessor> logger,
        IBank<User> tokenBank,
        IUserRepo userRepo,
        ISubscriptionLogRepo subscriptionLogRepo,
        ILinkedAccountRepo linkedAccountRepo,
        Duration subGiftProcessorDelay)
        : ISubscriptionProcessor
    {
        public async Task<ISubscriptionProcessor.SubResult> ProcessSubscription(SubscriptionInfo subscriptionInfo)
        {
            User user = subscriptionInfo.Subscriber;
            if (user.MonthsSubscribed > 0 && user.SubscriptionTier == null)
            {
                logger.LogInformation("Subscriber {User} has no subscription tier recorded. Assuming this user was " +
                                       "subscribed before tiers were a thing and is equivalent to Tier 1", user);
                user = await userRepo.SetSubscriptionInfo(
                    user, user.MonthsSubscribed, SubscriptionTier.Tier1, user.LoyaltyLeague,
                    user.SubscriptionUpdatedAt);
            }

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
                user = await userRepo.SetSubscriptionInfo(
                    user, subscriptionInfo.NumMonths - 1, subscriptionInfo.Tier, user.LoyaltyLeague,
                    user.SubscriptionUpdatedAt);
                subCountCorrected = true;
            }
            // If our internal months subscribed count is less than what Twitch says it is,
            // we update our count and give the subscriber "back pay" tokens.
            // This can occur when users (re)subscribe while the tpp bot is down,
            // or does not trigger the resubscription message for one or more months.

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

            await subscriptionLogRepo.LogSubscription(
                user.Id, subscriptionInfo.SubscriptionAt,
                subscriptionInfo.StreakMonths, user.MonthsSubscribed, subscriptionInfo.NumMonths, monthsDifference,
                oldLoyaltyLeague, newLoyaltyLeague, loyaltyCompletions, tokens, subscriptionInfo.IsGift,
                subscriptionInfo.Message, subscriptionInfo.Tier, subscriptionInfo.PlanName);

            await tokenBank.PerformTransaction(new Transaction<User>(
                user, tokens, TransactionType.Subscription, new Dictionary<string, object?>
                {
                    ["previous_months_subscribed"] = user.MonthsSubscribed,
                    ["new_months_subscribed"] = subscriptionInfo.NumMonths,
                    ["months_difference"] = monthsDifference,
                    ["previous_loyalty_tier"] = oldLoyaltyLeague,
                    ["new_loyalty_tier"] = newLoyaltyLeague,
                    ["loyalty_completions"] = loyaltyCompletions
                }));
            user = await userRepo.SetIsSubscribed(user, true);
            // ReSharper disable once RedundantAssignment : overwrite reference to keep usage of stale data impossible
            user = await userRepo.SetSubscriptionInfo(
                user, subscriptionInfo.NumMonths, subscriptionInfo.Tier, newLoyaltyLeague,
                subscriptionInfo.SubscriptionAt);
            return new ISubscriptionProcessor.SubResult.Ok(
                DeltaTokens: tokens,
                OldLoyaltyLeague: oldLoyaltyLeague,
                NewLoyaltyLeague: newLoyaltyLeague,
                CumulativeMonths: subscriptionInfo.NumMonths,
                SubCountCorrected: subCountCorrected);
        }

        public async Task<ISubscriptionProcessor.SubGiftResult> ProcessSubscriptionGift(
            SubscriptionGiftInfo subGiftInfo)
        {
            // EventSub does not give us any information to correlate sub events with sub gift events,
            // see https://twitch.uservoice.com/forums/310213-developers/suggestions/44500245-revise-the-channel-subscription-topics
            // This is a dirty workaround: Wait for some time to make sure the sub events get processed first,
            // and then just assume all recent gift subs are associated with this gift event.
            // This fails if e.g. multiple people simulatenously gift subs.
            List<User> recipients = [];
            await Task.Delay(subGiftProcessorDelay.ToTimeSpan());
            Instant cutoff = subGiftInfo.SubscriptionGiftAt - Duration.FromSeconds(10);
            List<string> recentGiftSubUserIDs = await subscriptionLogRepo.FindRecentGiftSubs(cutoff);
            foreach (string recipientUserId in recentGiftSubUserIDs)
            {
                User? user = await userRepo.FindById(recipientUserId);
                if (user == null)
                    logger.LogError("Unable to look up sub gift user by id, skipping: {UserId}", recipientUserId);
                else
                    recipients.Add(user);
            }

            if (recipients.Count < subGiftInfo.NumGifts)
            {
                logger.LogWarning(
                    "Processing gift subscription with fewer probable recipients than expected " +
                    "(processing gift message too early or too late?). " +
                    "Linked account detection may not detect all users as linked and withhold too few tokens. " +
                    "Expected {NumGifts} users, but received {NumUsers}: [{Users}]. Event: {Event}",
                    subGiftInfo.NumGifts, recipients.Count, string.Join(", ", recipients), subGiftInfo);
            }
            if (recipients.Count > subGiftInfo.NumGifts)
            {
                logger.LogWarning(
                    "Processing gift subscription with more probable recipients than expected " +
                    "(simultaneous sub gifts?). " +
                    "Linked account detection may detect too many linked accounts and withhold too many tokens. " +
                    "Expected {NumGifts} users, but received {NumUsers}: [{Users}]. Event: {Event}",
                    subGiftInfo.NumGifts, recipients.Count, string.Join(", ", recipients), subGiftInfo);
            }

            List<User> linkedRecipients = [];
            foreach (User recipient in recipients)
                if (await linkedAccountRepo.AreLinked(subGiftInfo.Gifter.Id, recipient.Id))
                    linkedRecipients.Add(recipient);

            const int tokensPerRank = 10;
            int tokenEligibleGifts = Math.Max(0, subGiftInfo.NumGifts - linkedRecipients.Count);
            int rewardTokens = tokenEligibleGifts * subGiftInfo.Tier.ToRank() * tokensPerRank;
            await tokenBank.PerformTransaction(new Transaction<User>(
                subGiftInfo.Gifter,
                rewardTokens,
                TransactionType.SubscriptionGift
            ));

            return linkedRecipients.Count == 0
                ? new ISubscriptionProcessor.SubGiftResult.Ok(rewardTokens, subGiftInfo.NumGifts)
                : new ISubscriptionProcessor.SubGiftResult.OkButLinked(rewardTokens, subGiftInfo.NumGifts,
                    linkedRecipients.ToImmutableList());
        }
    }
}
