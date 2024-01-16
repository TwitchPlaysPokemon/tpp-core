using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using TPP.Common;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests
{
    public class SubscriptionProcessorTest
    {
        private static User MockUser(string name, int monthsSubscribed, SubscriptionTier? subscriptionTier,
            int loyaltyLeague) => new User(
            id: Guid.NewGuid().ToString(),
            name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0,
            monthsSubscribed: monthsSubscribed, isSubscribed: subscriptionTier != null,
            subscriptionTier: subscriptionTier, loyaltyLeague: loyaltyLeague);

        [Test]
        public async Task handle_regular_subscription()
        {
            // GIVEN
            const SubscriptionTier subscriptionTier = SubscriptionTier.Tier2;
            Instant subscribedAt = Instant.FromUnixTimeSeconds(123);
            User user = MockUser("user", monthsSubscribed: 2, subscriptionTier, loyaltyLeague: 4);

            var bankMock = Substitute.For<IBank<User>>();
            var userRepoMock = Substitute.For<IUserRepo>();
            var subscriptionLogRepoMock = Substitute.For<ISubscriptionLogRepo>();
            ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
                NullLogger<SubscriptionProcessor>.Instance,
                bankMock, userRepoMock, subscriptionLogRepoMock, Substitute.For<ILinkedAccountRepo>());

            userRepoMock.SetIsSubscribed(user, Arg.Any<bool>()).Returns(user);
            userRepoMock.SetSubscriptionInfo(user, Arg.Any<int>(), Arg.Any<SubscriptionTier>(),
                Arg.Any<int>(), Arg.Any<Instant>()).Returns(user);

            // WHEN
            ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
                new SubscriptionInfo(
                    user, NumMonths: 3, StreakMonths: 2, subscriptionTier, PlanName: "Tier 2",
                    subscribedAt, Message: "HeyGuys", ImmutableList<EmoteOccurrence>.Empty));

            // THEN
            const int expectedTokens = 10 + (2 * 4) + 10 + (2 * 5); // per rank: 10 base tokens + 2 tokens per league
            // verify result
            Assert.That(subResult, Is.InstanceOf<ISubscriptionProcessor.SubResult.Ok>());
            var okResult = (ISubscriptionProcessor.SubResult.Ok)subResult;
            Assert.That(okResult.CumulativeMonths, Is.EqualTo(3));
            Assert.That(okResult.DeltaTokens, Is.EqualTo(expectedTokens));
            Assert.That(okResult.OldLoyaltyLeague, Is.EqualTo(4));
            Assert.That(okResult.NewLoyaltyLeague, Is.EqualTo(6));
            Assert.That(okResult.SubCountCorrected, Is.False);

            // verify tokens were awarded
            IDictionary<string, object?> expectedData = new Dictionary<string, object?>
            {
                ["previous_months_subscribed"] = 2,
                ["new_months_subscribed"] = 3,
                ["months_difference"] = 1,
                ["previous_loyalty_tier"] = 4,
                ["new_loyalty_tier"] = 6,
                ["loyalty_completions"] = 2,
            };
            await bankMock.Received(1).PerformTransaction(
                new Transaction<User>(user, expectedTokens, "subscription", expectedData), CancellationToken.None);

            // verify user data was adjusted
            await userRepoMock.Received(1).SetIsSubscribed(user, true);
            await userRepoMock.Received(1).SetSubscriptionInfo(user, 3, subscriptionTier, 6, subscribedAt);

            // verify subscription was logged
            await subscriptionLogRepoMock.Received(1).LogSubscription(
                user.Id, subscribedAt,
                2, 2, 3, 1,
                4, 6, 2, expectedTokens,
                "HeyGuys", subscriptionTier, "Tier 2");
        }

        [Test]
        public async Task ignore_duplicate_month_same_rank()
        {
            // GIVEN
            User user = MockUser("user", monthsSubscribed: 2, SubscriptionTier.Tier1, loyaltyLeague: 2);
            var bankMock = Substitute.For<IBank<User>>();
            var userRepoMock = Substitute.For<IUserRepo>();
            var subscriptionLogRepoMock = Substitute.For<ISubscriptionLogRepo>();
            ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
                NullLogger<SubscriptionProcessor>.Instance,
                bankMock, userRepoMock, subscriptionLogRepoMock, Substitute.For<ILinkedAccountRepo>());

            // WHEN
            ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
                new SubscriptionInfo(
                    user, NumMonths: 2, StreakMonths: 2, SubscriptionTier.Tier1, PlanName: "Sub Plan Name",
                    Instant.MinValue, Message: "Repeated", ImmutableList<EmoteOccurrence>.Empty));

            // THEN
            // negative result
            Assert.That(subResult, Is.InstanceOf<ISubscriptionProcessor.SubResult.SameMonth>());
            var sameMonthResult = (ISubscriptionProcessor.SubResult.SameMonth)subResult;
            Assert.That(sameMonthResult.Month, Is.EqualTo(2));
            // no tokens were awarded
            Assert.That(bankMock.ReceivedCalls().Count(), Is.EqualTo(0));
            // no user data was adjusted
            await userRepoMock.DidNotReceive().SetIsSubscribed(Arg.Any<User>(), Arg.Any<bool>());
            await userRepoMock.DidNotReceive().SetSubscriptionInfo(Arg.Any<User>(), Arg.Any<int>(),
                Arg.Any<SubscriptionTier>(), Arg.Any<int>(), Arg.Any<Instant>());
            // no subscription was logged
            Assert.That(subscriptionLogRepoMock.ReceivedCalls().Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task accept_duplicate_month_higher_rank()
        {
            // GIVEN
            User user = MockUser("user", monthsSubscribed: 2, SubscriptionTier.Prime, loyaltyLeague: 2);
            Instant subscribedAt = Instant.FromUnixTimeSeconds(123);
            var bankMock = Substitute.For<IBank<User>>();
            var userRepoMock = Substitute.For<IUserRepo>();
            var subscriptionLogRepoMock = Substitute.For<ISubscriptionLogRepo>();
            ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
                NullLogger<SubscriptionProcessor>.Instance,
                bankMock, userRepoMock, subscriptionLogRepoMock, Substitute.For<ILinkedAccountRepo>());

            userRepoMock.SetIsSubscribed(user, Arg.Any<bool>()).Returns(user);
            userRepoMock.SetSubscriptionInfo(user, Arg.Any<int>(), Arg.Any<SubscriptionTier>(),
                Arg.Any<int>(), Arg.Any<Instant>()).Returns(user);

            // WHEN
            ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
                new SubscriptionInfo(
                    user, NumMonths: 2, StreakMonths: 2, SubscriptionTier.Tier3, PlanName: "Sub Plan Name",
                    subscribedAt, Message: "Repeated", ImmutableList<EmoteOccurrence>.Empty));

            // THEN
            const int expectedTokens = 14 + 16 + 18 + 20; // Tier 1 -> Tier 3: 4 loyalty completions difference
            // negative result
            Assert.That(subResult, Is.InstanceOf<ISubscriptionProcessor.SubResult.Ok>());
            var okResult = (ISubscriptionProcessor.SubResult.Ok)subResult;
            Assert.That(okResult.NewLoyaltyLeague, Is.EqualTo(6));
            Assert.That(okResult.CumulativeMonths, Is.EqualTo(2));
            // only tokens for rank upgrade were awarded
            IDictionary<string, object?> expectedData = new Dictionary<string, object?>
            {
                ["previous_months_subscribed"] = 2,
                ["new_months_subscribed"] = 2,
                ["months_difference"] = 0,
                ["previous_loyalty_tier"] = 2,
                ["new_loyalty_tier"] = 6,
                ["loyalty_completions"] = 4,
            };
            await bankMock.Received(1).PerformTransaction(
                new Transaction<User>(user, expectedTokens, "subscription", expectedData),
                CancellationToken.None);
            // verify user data was adjusted
            await userRepoMock.Received(1).SetIsSubscribed(user, true);
            await userRepoMock.Received(1).SetSubscriptionInfo(user, 2, SubscriptionTier.Tier3, 6, subscribedAt);
            // verify subscription was logged
            await subscriptionLogRepoMock.Received(1).LogSubscription(
                user.Id, subscribedAt,
                2, 2, 2, 0,
                2, 6, 4, expectedTokens,
                "Repeated", SubscriptionTier.Tier3, "Sub Plan Name");
        }

        [Test]
        public async Task handle_sub_gift_and_reward_gift_tokens()
        {
            User gifter = MockUser("gifter", monthsSubscribed: 2, SubscriptionTier.Prime, loyaltyLeague: 2);
            User recipient = MockUser("recipient", monthsSubscribed: 0, subscriptionTier: null, loyaltyLeague: 0);
            var bankMock = Substitute.For<IBank<User>>();
            var userRepoMock = Substitute.For<IUserRepo>();
            ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
                NullLogger<SubscriptionProcessor>.Instance,
                bankMock, userRepoMock, Substitute.For<ISubscriptionLogRepo>(), Substitute.For<ILinkedAccountRepo>());
            userRepoMock.SetIsSubscribed(recipient, Arg.Any<bool>()).Returns(recipient);
            userRepoMock.SetSubscriptionInfo(recipient, Arg.Any<int>(), Arg.Any<SubscriptionTier>(),
                Arg.Any<int>(), Arg.Any<Instant>()).Returns(recipient);

            SubscriptionInfo subscriptionInfo = new(recipient, 1, 0, SubscriptionTier.Tier3, "Sub Plan Name",
                Instant.MinValue, "sub message", ImmutableList<EmoteOccurrence>.Empty);
            (ISubscriptionProcessor.SubResult subResult, ISubscriptionProcessor.SubGiftResult subGiftResult) =
                await subscriptionProcessor.ProcessSubscriptionGift(
                    new SubscriptionGiftInfo(subscriptionInfo, gifter, 2, false));

            const int expectedGiftTokens = 10 * 5 * 2; // 10 per rank. Tier 3 has rank 5 because $25 = 5 * $5, 2 months
            Assert.That(subGiftResult, Is.InstanceOf<ISubscriptionProcessor.SubGiftResult.Ok>());
            var okGiftResult = (ISubscriptionProcessor.SubGiftResult.Ok)subGiftResult;
            Assert.That(okGiftResult.GifterTokens, Is.EqualTo(expectedGiftTokens));
            IDictionary<string, object?> expectedGiftData = new Dictionary<string, object?>();
            await bankMock.Received(1).PerformTransaction(
                new Transaction<User>(gifter, expectedGiftTokens, "subscription gift", expectedGiftData),
                CancellationToken.None);

            const int expectedSubTokens = 10 + 12 + 14 + 16 + 18; // Tier 3 = 5 ranks with increasing loyalty league
            Assert.That(subResult, Is.InstanceOf<ISubscriptionProcessor.SubResult.Ok>());
            var okResult = (ISubscriptionProcessor.SubResult.Ok)subResult;
            Assert.That(okResult.CumulativeMonths, Is.EqualTo(1));
            Assert.That(okResult.DeltaTokens, Is.EqualTo(expectedSubTokens));
            Assert.That(okResult.OldLoyaltyLeague, Is.EqualTo(0));
            Assert.That(okResult.NewLoyaltyLeague, Is.EqualTo(5));
            Assert.That(okResult.SubCountCorrected, Is.False);
            IDictionary<string, object?> expectedSubData = new Dictionary<string, object?>
            {
                ["previous_months_subscribed"] = 0,
                ["new_months_subscribed"] = 1,
                ["months_difference"] = 1,
                ["previous_loyalty_tier"] = 0,
                ["new_loyalty_tier"] = 5,
                ["loyalty_completions"] = 5,
            };
            await bankMock.Received(1).PerformTransaction(
                new Transaction<User>(recipient, expectedSubTokens, "subscription", expectedSubData),
                CancellationToken.None);
        }

        [Test]
        public async Task ignore_duplicate_month_for_sub_gift()
        {
            User gifter = MockUser("gifter", monthsSubscribed: 2, SubscriptionTier.Prime, loyaltyLeague: 2);
            const SubscriptionTier tier = SubscriptionTier.Tier3;
            User recipient = MockUser("recipient", monthsSubscribed: 1, subscriptionTier: tier, loyaltyLeague: 5);
            var bankMock = Substitute.For<IBank<User>>();
            var userRepoMock = Substitute.For<IUserRepo>();
            ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
                NullLogger<SubscriptionProcessor>.Instance,
                bankMock, userRepoMock, Substitute.For<ISubscriptionLogRepo>(), Substitute.For<ILinkedAccountRepo>());

            SubscriptionInfo subscriptionInfo = new(recipient, NumMonths: 1, StreakMonths: 0, tier, "Sub Plan Name",
                Instant.MinValue, "sub message", ImmutableList<EmoteOccurrence>.Empty);
            (ISubscriptionProcessor.SubResult subResult, ISubscriptionProcessor.SubGiftResult subGiftResult) =
                await subscriptionProcessor.ProcessSubscriptionGift(
                    new SubscriptionGiftInfo(subscriptionInfo, gifter, 1, false));

            Assert.That(subGiftResult, Is.InstanceOf<ISubscriptionProcessor.SubGiftResult.SameMonth>());
            var sameMonthGiftResult = (ISubscriptionProcessor.SubGiftResult.SameMonth)subGiftResult;
            Assert.That(sameMonthGiftResult.Month, Is.EqualTo(1));
            Assert.That(bankMock.ReceivedCalls().Count(), Is.EqualTo(0));

            Assert.That(subResult, Is.InstanceOf<ISubscriptionProcessor.SubResult.SameMonth>());
            var sameMonthSubResult = (ISubscriptionProcessor.SubResult.SameMonth)subResult;
            Assert.That(sameMonthSubResult.Month, Is.EqualTo(1));
            Assert.That(bankMock.ReceivedCalls().Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task gracefully_handle_subbed_user_without_sub_tier_same_month()
        {
            // GIVEN
            Instant subscribedAt = Instant.FromUnixTimeSeconds(123);
            User userWithoutTier = MockUser("user", monthsSubscribed: 1, null, loyaltyLeague: 1);
            User userWithTier = MockUser("user", monthsSubscribed: 1, SubscriptionTier.Tier1, loyaltyLeague: 1);

            var logMock = Substitute.For<VerifiableMockLogger<SubscriptionProcessor>>();
            var userRepoMock = Substitute.For<IUserRepo>();
            var subscriptionLogRepoMock = Substitute.For<ISubscriptionLogRepo>();
            ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
                logMock, Substitute.For<IBank<User>>(), userRepoMock,
                subscriptionLogRepoMock, Substitute.For<ILinkedAccountRepo>());
            userRepoMock.SetSubscriptionInfo(userWithoutTier, Arg.Any<int>(), Arg.Any<SubscriptionTier>(),
                Arg.Any<int>(), Arg.Any<Instant?>()).Returns(userWithTier);

            // WHEN
            ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
                new SubscriptionInfo(
                    userWithoutTier, NumMonths: 1, StreakMonths: 1, SubscriptionTier.Tier1, PlanName: "Tier 1",
                    subscribedAt, Message: "HeyGuys", ImmutableList<EmoteOccurrence>.Empty));

            // THEN
            Assert.That(subResult, Is.InstanceOf<ISubscriptionProcessor.SubResult.SameMonth>());
            logMock.Received(1).Log(LogLevel.Information,
                $"Subscriber {userWithoutTier} has no subscription tier recorded. " +
                "Assuming this user was subscribed before tiers were a thing and is equivalent to Tier 1");
            await userRepoMock.Received(1).SetSubscriptionInfo(userWithoutTier, 1,
                SubscriptionTier.Tier1, userWithoutTier.LoyaltyLeague, userWithoutTier.SubscriptionUpdatedAt);
        }
    }
}
