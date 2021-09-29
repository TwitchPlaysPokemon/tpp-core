using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Common;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core;

public record EmoteOccurrence(string Id, string Code, int StartIndex, int EndIndex);

/// <summary>
/// Information on a user subscription directly (not via a gift).
/// Gifted subscriptions are using <see cref="SubscriptionGiftInfo"/> instead.
/// </summary>
public record SubscriptionInfo(
    User Subscriber,
    int NumMonths,
    int StreakMonths,
    SubscriptionTier Tier,
    string PlanName,
    Instant SubscriptionAt,
    string? Message,
    IImmutableList<EmoteOccurrence> Emotes);

/// Information on a user subscribing through a gifted subscription.
public record SubscriptionGiftInfo(
    SubscriptionInfo SubscriptionInfo, User Gifter, int NumGiftedMonths, bool IsAnonymous);

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
        public sealed record Ok(int GifterTokens) : SubGiftResult;
        /// The gifter is linked to the gift recipient and has not received a token reward
        public sealed record LinkedAccount : SubGiftResult;
        /// The subscription was deemed a duplicate and the gifter has not received a token reward
        public sealed record SameMonth(int Month) : SubGiftResult;
    }

    Task<SubResult> ProcessSubscription(SubscriptionInfo subscriptionInfo);
    Task<(SubResult, SubGiftResult)> ProcessSubscriptionGift(SubscriptionGiftInfo subscriptionGiftInfo);
}

public class SubscriptionProcessor : ISubscriptionProcessor
{
    private readonly ILogger<SubscriptionProcessor> _logger;
    private readonly IBank<User> _tokenBank;
    private readonly IUserRepo _userRepo;
    private readonly ISubscriptionLogRepo _subscriptionLogRepo;
    private readonly ILinkedAccountRepo _linkedAccountRepo;

    public SubscriptionProcessor(
        ILogger<SubscriptionProcessor> logger,
        IBank<User> tokenBank, IUserRepo userRepo, ISubscriptionLogRepo subscriptionLogRepo,
        ILinkedAccountRepo linkedAccountRepo)
    {
        _logger = logger;
        _tokenBank = tokenBank;
        _userRepo = userRepo;
        _subscriptionLogRepo = subscriptionLogRepo;
        _linkedAccountRepo = linkedAccountRepo;
    }

    public async Task<ISubscriptionProcessor.SubResult> ProcessSubscription(SubscriptionInfo subscriptionInfo)
    {
        User user = subscriptionInfo.Subscriber;
        if (user.MonthsSubscribed > 0 && user.SubscriptionTier == null)
        {
            _logger.LogWarning("Subscriber {User} has no subscription tier recorded. Assuming Tier 1", user);
            user = await _userRepo.SetSubscriptionInfo(
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

        await _subscriptionLogRepo.LogSubscription(
            user.Id, subscriptionInfo.SubscriptionAt,
            subscriptionInfo.StreakMonths, user.MonthsSubscribed, subscriptionInfo.NumMonths, monthsDifference,
            oldLoyaltyLeague, newLoyaltyLeague, loyaltyCompletions, tokens,
            subscriptionInfo.Message, subscriptionInfo.Tier, subscriptionInfo.PlanName);

        await _tokenBank.PerformTransaction(new Transaction<User>(
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
        // ReSharper disable once RedundantAssignment : overwrite reference to keep usage of stale data impossible
        user = await _userRepo.SetSubscriptionInfo(
            user, subscriptionInfo.NumMonths, subscriptionInfo.Tier, newLoyaltyLeague,
            subscriptionInfo.SubscriptionAt);
        return new ISubscriptionProcessor.SubResult.Ok(
            DeltaTokens: tokens,
            OldLoyaltyLeague: oldLoyaltyLeague,
            NewLoyaltyLeague: newLoyaltyLeague,
            CumulativeMonths: subscriptionInfo.NumMonths,
            SubCountCorrected: subCountCorrected);
    }

    public async Task<(ISubscriptionProcessor.SubResult, ISubscriptionProcessor.SubGiftResult)> ProcessSubscriptionGift(
        SubscriptionGiftInfo subscriptionGiftInfo)
    {
        SubscriptionInfo subscriptionInfo = subscriptionGiftInfo.SubscriptionInfo;
        ISubscriptionProcessor.SubResult subResult =
            await ProcessSubscription(subscriptionInfo);
        bool isLinkedAccount = await _linkedAccountRepo.AreLinked(
            subscriptionGiftInfo.Gifter.Id,
            subscriptionInfo.Subscriber.Id);

        if (isLinkedAccount)
            return (subResult, new ISubscriptionProcessor.SubGiftResult.LinkedAccount());

        if (subResult is ISubscriptionProcessor.SubResult.SameMonth { Month: var month })
            return (subResult, new ISubscriptionProcessor.SubGiftResult.SameMonth(month));

        const int tokensPerRank = 10;
        int rewardTokens = subscriptionGiftInfo.NumGiftedMonths * subscriptionInfo.Tier.ToRank() * tokensPerRank;
        await _tokenBank.PerformTransaction(new Transaction<User>(
            subscriptionGiftInfo.Gifter,
            rewardTokens,
            TransactionType.SubscriptionGift
        ));

        return (subResult, new ISubscriptionProcessor.SubGiftResult.Ok(rewardTokens));
    }
}
