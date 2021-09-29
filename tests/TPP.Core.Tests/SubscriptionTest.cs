using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.Common;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests;

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
            NullLogger<SubscriptionProcessor>.Instance,
            bankMock.Object, userRepoMock.Object, subscriptionLogRepoMock.Object, Mock.Of<ILinkedAccountRepo>());

        userRepoMock.Setup(r => r.SetIsSubscribed(user, It.IsAny<bool>())).ReturnsAsync(user);
        userRepoMock.Setup(r => r.SetSubscriptionInfo(user, It.IsAny<int>(), It.IsAny<SubscriptionTier>(),
            It.IsAny<int>(), It.IsAny<Instant>())).ReturnsAsync(user);

        // WHEN
        ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
            new SubscriptionInfo(
                user, NumMonths: 3, StreakMonths: 2, subscriptionTier, PlanName: "Tier 2",
                subscribedAt, Message: "HeyGuys", ImmutableList<EmoteOccurrence>.Empty));

        // THEN
        const int expectedTokens = 10 + (2 * 4) + 10 + (2 * 5); // per rank: 10 base tokens + 2 tokens per league
        // verify result
        Assert.IsInstanceOf<ISubscriptionProcessor.SubResult.Ok>(subResult);
        var okResult = (ISubscriptionProcessor.SubResult.Ok)subResult;
        Assert.That(okResult.CumulativeMonths, Is.EqualTo(3));
        Assert.That(okResult.DeltaTokens, Is.EqualTo(expectedTokens));
        Assert.That(okResult.OldLoyaltyLeague, Is.EqualTo(4));
        Assert.That(okResult.NewLoyaltyLeague, Is.EqualTo(6));
        Assert.IsFalse(okResult.SubCountCorrected);

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
            NullLogger<SubscriptionProcessor>.Instance,
            bankMock.Object, userRepoMock.Object, subscriptionLogRepoMock.Object, Mock.Of<ILinkedAccountRepo>());

        // WHEN
        ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
            new SubscriptionInfo(
                user, NumMonths: 2, StreakMonths: 2, SubscriptionTier.Tier1, PlanName: "Sub Plan Name",
                Instant.MinValue, Message: "Repeated", ImmutableList<EmoteOccurrence>.Empty));

        // THEN
        // negative result
        Assert.IsInstanceOf<ISubscriptionProcessor.SubResult.SameMonth>(subResult);
        var sameMonthResult = (ISubscriptionProcessor.SubResult.SameMonth)subResult;
        Assert.That(sameMonthResult.Month, Is.EqualTo(2));
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
            NullLogger<SubscriptionProcessor>.Instance,
            bankMock.Object, userRepoMock.Object, subscriptionLogRepoMock.Object, Mock.Of<ILinkedAccountRepo>());

        userRepoMock.Setup(r => r.SetIsSubscribed(user, It.IsAny<bool>())).ReturnsAsync(user);
        userRepoMock.Setup(r => r.SetSubscriptionInfo(user, It.IsAny<int>(), It.IsAny<SubscriptionTier>(),
            It.IsAny<int>(), It.IsAny<Instant>())).ReturnsAsync(user);

        // WHEN
        ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
            new SubscriptionInfo(
                user, NumMonths: 2, StreakMonths: 2, SubscriptionTier.Tier3, PlanName: "Sub Plan Name",
                subscribedAt, Message: "Repeated", ImmutableList<EmoteOccurrence>.Empty));

        // THEN
        const int expectedTokens = 14 + 16 + 18 + 20; // Tier 1 -> Tier 3: 4 loyalty completions difference
        // negative result
        Assert.IsInstanceOf<ISubscriptionProcessor.SubResult.Ok>(subResult);
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
    public async Task handle_sub_gift_and_reward_gift_tokens()
    {
        User gifter = MockUser("gifter", monthsSubscribed: 2, SubscriptionTier.Prime, loyaltyLeague: 2);
        User recipient = MockUser("recipient", monthsSubscribed: 0, subscriptionTier: null, loyaltyLeague: 0);
        Mock<IBank<User>> bankMock = new();
        Mock<IUserRepo> userRepoMock = new();
        ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
            NullLogger<SubscriptionProcessor>.Instance,
            bankMock.Object, userRepoMock.Object, Mock.Of<ISubscriptionLogRepo>(), Mock.Of<ILinkedAccountRepo>());
        userRepoMock.Setup(r => r.SetIsSubscribed(recipient, It.IsAny<bool>())).ReturnsAsync(recipient);
        userRepoMock.Setup(r => r.SetSubscriptionInfo(recipient, It.IsAny<int>(), It.IsAny<SubscriptionTier>(),
            It.IsAny<int>(), It.IsAny<Instant>())).ReturnsAsync(recipient);

        SubscriptionInfo subscriptionInfo = new(recipient, 1, 0, SubscriptionTier.Tier3, "Sub Plan Name",
            Instant.MinValue, "sub message", ImmutableList<EmoteOccurrence>.Empty);
        (ISubscriptionProcessor.SubResult subResult, ISubscriptionProcessor.SubGiftResult subGiftResult) =
            await subscriptionProcessor.ProcessSubscriptionGift(
                new SubscriptionGiftInfo(subscriptionInfo, gifter, 2, false));

        const int expectedGiftTokens = 10 * 5 * 2; // 10 per rank. Tier 3 has rank 5 because $25 = 5 * $5, 2 months
        Assert.IsInstanceOf<ISubscriptionProcessor.SubGiftResult.Ok>(subGiftResult);
        var okGiftResult = (ISubscriptionProcessor.SubGiftResult.Ok)subGiftResult;
        Assert.That(okGiftResult.GifterTokens, Is.EqualTo(expectedGiftTokens));
        IDictionary<string, object?> expectedGiftData = new Dictionary<string, object?>();
        bankMock.Verify(b => b.PerformTransaction(
            new Transaction<User>(gifter, expectedGiftTokens, "subscription gift", expectedGiftData),
            CancellationToken.None), Times.Once);

        const int expectedSubTokens = 10 + 12 + 14 + 16 + 18; // Tier 3 = 5 ranks with increasing loyalty league
        Assert.IsInstanceOf<ISubscriptionProcessor.SubResult.Ok>(subResult);
        var okResult = (ISubscriptionProcessor.SubResult.Ok)subResult;
        Assert.That(okResult.CumulativeMonths, Is.EqualTo(1));
        Assert.That(okResult.DeltaTokens, Is.EqualTo(expectedSubTokens));
        Assert.That(okResult.OldLoyaltyLeague, Is.EqualTo(0));
        Assert.That(okResult.NewLoyaltyLeague, Is.EqualTo(5));
        Assert.IsFalse(okResult.SubCountCorrected);
        IDictionary<string, object?> expectedSubData = new Dictionary<string, object?>
        {
            ["previous_months_subscribed"] = 0,
            ["new_months_subscribed"] = 1,
            ["months_difference"] = 1,
            ["previous_loyalty_tier"] = 0,
            ["new_loyalty_tier"] = 5,
            ["loyalty_completions"] = 5,
        };
        bankMock.Verify(b => b.PerformTransaction(
            new Transaction<User>(recipient, expectedSubTokens, "subscription", expectedSubData),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task ignore_duplicate_month_for_sub_gift()
    {
        User gifter = MockUser("gifter", monthsSubscribed: 2, SubscriptionTier.Prime, loyaltyLeague: 2);
        const SubscriptionTier tier = SubscriptionTier.Tier3;
        User recipient = MockUser("recipient", monthsSubscribed: 1, subscriptionTier: tier, loyaltyLeague: 5);
        Mock<IBank<User>> bankMock = new();
        Mock<IUserRepo> userRepoMock = new();
        ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
            NullLogger<SubscriptionProcessor>.Instance,
            bankMock.Object, userRepoMock.Object, Mock.Of<ISubscriptionLogRepo>(), Mock.Of<ILinkedAccountRepo>());

        SubscriptionInfo subscriptionInfo = new(recipient, NumMonths: 1, StreakMonths: 0, tier, "Sub Plan Name",
            Instant.MinValue, "sub message", ImmutableList<EmoteOccurrence>.Empty);
        (ISubscriptionProcessor.SubResult subResult, ISubscriptionProcessor.SubGiftResult subGiftResult) =
            await subscriptionProcessor.ProcessSubscriptionGift(
                new SubscriptionGiftInfo(subscriptionInfo, gifter, 1, false));

        Assert.IsInstanceOf<ISubscriptionProcessor.SubGiftResult.SameMonth>(subGiftResult);
        var sameMonthGiftResult = (ISubscriptionProcessor.SubGiftResult.SameMonth)subGiftResult;
        Assert.That(sameMonthGiftResult.Month, Is.EqualTo(1));
        bankMock.VerifyNoOtherCalls();

        Assert.IsInstanceOf<ISubscriptionProcessor.SubResult.SameMonth>(subResult);
        var sameMonthSubResult = (ISubscriptionProcessor.SubResult.SameMonth)subResult;
        Assert.That(sameMonthSubResult.Month, Is.EqualTo(1));
        bankMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task gracefully_handle_subbed_user_without_sub_tier_same_month()
    {
        // GIVEN
        Instant subscribedAt = Instant.FromUnixTimeSeconds(123);
        User userWithoutTier = MockUser("user", monthsSubscribed: 1, null, loyaltyLeague: 1);
        User userWithTier = MockUser("user", monthsSubscribed: 1, SubscriptionTier.Tier1, loyaltyLeague: 1);

        Mock<ILogger<SubscriptionProcessor>> logMock = new();
        Mock<IUserRepo> userRepoMock = new();
        Mock<ISubscriptionLogRepo> subscriptionLogRepoMock = new();
        ISubscriptionProcessor subscriptionProcessor = new SubscriptionProcessor(
            logMock.Object, Mock.Of<IBank<User>>(), userRepoMock.Object,
            subscriptionLogRepoMock.Object, Mock.Of<ILinkedAccountRepo>());
        userRepoMock.Setup(r => r.SetSubscriptionInfo(userWithoutTier, It.IsAny<int>(), It.IsAny<SubscriptionTier>(),
            It.IsAny<int>(), It.IsAny<Instant?>())).ReturnsAsync(userWithTier);

        // WHEN
        ISubscriptionProcessor.SubResult subResult = await subscriptionProcessor.ProcessSubscription(
            new SubscriptionInfo(
                userWithoutTier, NumMonths: 1, StreakMonths: 1, SubscriptionTier.Tier1, PlanName: "Tier 1",
                subscribedAt, Message: "HeyGuys", ImmutableList<EmoteOccurrence>.Empty));

        // THEN
        Assert.IsInstanceOf<ISubscriptionProcessor.SubResult.SameMonth>(subResult);
        logMock.VerifyLog(l => l.LogWarning(
            $"Subscriber {userWithoutTier} has no subscription tier recorded. Assuming Tier 1"));
        userRepoMock.Verify(r => r.SetSubscriptionInfo(userWithoutTier, 1,
            SubscriptionTier.Tier1, userWithoutTier.LoyaltyLeague, userWithoutTier.SubscriptionUpdatedAt));
    }
}
