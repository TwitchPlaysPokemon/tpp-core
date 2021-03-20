using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

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

            Mock<IBank<User>> bankMock = new();
            Mock<IUserRepo> userRepoMock = new();
            Mock<ISubscriptionLogRepo> subscriptionLogRepoMock = new();
            ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
                bankMock.Object, userRepoMock.Object, subscriptionLogRepoMock.Object);

            userRepoMock.Setup(r => r.SetIsSubscribed(user, It.IsAny<bool>())).ReturnsAsync(user);
            userRepoMock.Setup(r => r.SetSubscriptionInfo(user, It.IsAny<int>(), It.IsAny<SubscriptionTier>(),
                It.IsAny<int>(), It.IsAny<Instant>())).ReturnsAsync(user);

            // WHEN
            ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
                new SubscriptionInfo(
                    user, NumMonths: 3, StreakMonths: 2, subscriptionTier, PlanName: "Tier 2",
                    subscribedAt, Message: "HeyGuys", Gifter: null, IsAnonymous: false));

            // THEN
            const int expectedTokens = 10 + (2 * 4) + 10 + (2 * 5); // per rank: 10 base tokens + 2 tokens per league
            // verify result
            Assert.IsInstanceOf<ISubscriptionProcessor.SubResult.Ok>(subResult);
            var okResult = (ISubscriptionProcessor.SubResult.Ok)subResult;
            Assert.AreEqual(3, okResult.CumulativeMonths);
            Assert.AreEqual(expectedTokens, okResult.DeltaTokens);
            Assert.AreEqual(4, okResult.OldLoyaltyLeague);
            Assert.AreEqual(6, okResult.NewLoyaltyLeague);
            Assert.IsFalse(okResult.SubCountCorrected);
            Assert.IsNull(okResult.Gifter);

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
            bankMock.Verify(b =>
                b.PerformTransaction(new Transaction<User>(user, expectedTokens, "subscription", expectedData),
                    CancellationToken.None), Times.Once);

            // verify user data was adjusted
            userRepoMock.Verify(r => r.SetIsSubscribed(user, true), Times.Once);
            userRepoMock.Verify(r => r.SetSubscriptionInfo(user, 3, subscriptionTier, 6, subscribedAt), Times.Once);

            // verify subscription was logged
            subscriptionLogRepoMock.Verify(r => r.LogSubscription(
                    user.Id, subscribedAt,
                    2, 2, 3, 1,
                    4, 6, 2, expectedTokens,
                    "HeyGuys", subscriptionTier, "Tier 2"),
                Times.Once);
        }

        [Test]
        public async Task ignore_duplicate_month_same_rank()
        {
            // GIVEN
            User user = MockUser("user", monthsSubscribed: 2, SubscriptionTier.Tier1, loyaltyLeague: 2);
            Mock<IBank<User>> bankMock = new();
            Mock<IUserRepo> userRepoMock = new();
            Mock<ISubscriptionLogRepo> subscriptionLogRepoMock = new();
            ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
                bankMock.Object, userRepoMock.Object, subscriptionLogRepoMock.Object);

            // WHEN
            ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
                new SubscriptionInfo(
                    user, NumMonths: 2, StreakMonths: 2, SubscriptionTier.Tier1, PlanName: "Sub Plan Name",
                    Instant.MinValue, Message: "Repeated", Gifter: null, IsAnonymous: false));

            // THEN
            // negative result
            Assert.IsInstanceOf<ISubscriptionProcessor.SubResult.SameMonth>(subResult);
            var sameMonthResult = (ISubscriptionProcessor.SubResult.SameMonth)subResult;
            Assert.AreEqual(2, sameMonthResult.Month);
            // no tokens were awarded
            bankMock.VerifyNoOtherCalls();
            // no user data was adjusted
            userRepoMock.Verify(r => r.SetIsSubscribed(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
            userRepoMock.Verify(r => r.SetSubscriptionInfo(It.IsAny<User>(), It.IsAny<int>(),
                It.IsAny<SubscriptionTier>(), It.IsAny<int>(), It.IsAny<Instant>()), Times.Never);
            // no subscription was logged
            subscriptionLogRepoMock.VerifyNoOtherCalls();
        }

        [Test]
        public async Task accept_duplicate_month_higher_rank()
        {
            // GIVEN
            User user = MockUser("user", monthsSubscribed: 2, SubscriptionTier.Prime, loyaltyLeague: 2);
            Instant subscribedAt = Instant.FromUnixTimeSeconds(123);
            Mock<IBank<User>> bankMock = new();
            Mock<IUserRepo> userRepoMock = new();
            Mock<ISubscriptionLogRepo> subscriptionLogRepoMock = new();
            ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
                bankMock.Object, userRepoMock.Object, subscriptionLogRepoMock.Object);

            userRepoMock.Setup(r => r.SetIsSubscribed(user, It.IsAny<bool>())).ReturnsAsync(user);
            userRepoMock.Setup(r => r.SetSubscriptionInfo(user, It.IsAny<int>(), It.IsAny<SubscriptionTier>(),
                It.IsAny<int>(), It.IsAny<Instant>())).ReturnsAsync(user);

            // WHEN
            ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
                new SubscriptionInfo(
                    user, NumMonths: 2, StreakMonths: 2, SubscriptionTier.Tier3, PlanName: "Sub Plan Name",
                    subscribedAt, Message: "Repeated", Gifter: null, IsAnonymous: false));

            // THEN
            const int expectedTokens = 14 + 16 + 18 + 20; // Tier 1 -> Tier 3: 4 loyalty completions difference
            // negative result
            Assert.IsInstanceOf<ISubscriptionProcessor.SubResult.Ok>(subResult);
            var okResult = (ISubscriptionProcessor.SubResult.Ok)subResult;
            Assert.AreEqual(6, okResult.NewLoyaltyLeague);
            Assert.AreEqual(2, okResult.CumulativeMonths);
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
            bankMock.Verify(b => b.PerformTransaction(
                new Transaction<User>(user, expectedTokens, "subscription", expectedData),
                CancellationToken.None), Times.Once);
            // verify user data was adjusted
            userRepoMock.Verify(r => r.SetIsSubscribed(user, true), Times.Once);
            userRepoMock.Verify(r => r.SetSubscriptionInfo(user, 2, SubscriptionTier.Tier3, 6, subscribedAt),
                Times.Once);
            // verify subscription was logged
            subscriptionLogRepoMock.Verify(r => r.LogSubscription(
                    user.Id, subscribedAt,
                    2, 2, 2, 0,
                    2, 6, 4, expectedTokens,
                    "Repeated", SubscriptionTier.Tier3, "Sub Plan Name"),
                Times.Once);
        }

        [Test]
        public async Task reward_sub_gift_tokens()
        {
            // GIVEN
            User user = MockUser("user", monthsSubscribed: 2, SubscriptionTier.Prime, loyaltyLeague: 2);
            Mock<IBank<User>> bankMock = new();
            ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
                bankMock.Object, Mock.Of<IUserRepo>(), Mock.Of<ISubscriptionLogRepo>());

            ISubscriptionProcessor.SubGiftResult subGiftResult = await subscriptionProcessor.ProcessSubscriptionGift(
                new SubscriptionGiftInfo(user, SubscriptionTier.Tier3, false));

            const int expectedTokens = 10 * 5; // 10 per rank. Tier 3 has rank 5 because $25 = 5 * $5
            Assert.IsInstanceOf<ISubscriptionProcessor.SubGiftResult.Ok>(subGiftResult);
            var okResult = (ISubscriptionProcessor.SubGiftResult.Ok)subGiftResult;
            Assert.AreEqual(expectedTokens, okResult.DeltaTokens);
            Assert.AreEqual(false, okResult.IsAnonymous);
            IDictionary<string, object?> expectedData = new Dictionary<string, object?>();
            bankMock.Verify(b => b.PerformTransaction(
                new Transaction<User>(user, expectedTokens, "subscription gift", expectedData),
                CancellationToken.None), Times.Once);
        }
    }
}
