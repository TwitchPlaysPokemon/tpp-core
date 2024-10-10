using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using Common;
using Model;
using PersistenceMongoDB.Repos;

namespace PersistenceMongoDB.Tests.Repos;

public class LogRepoTests : MongoTestBase
{
    [Test]
    public async Task CommandLogger()
    {
        var clock = Substitute.For<IClock>();
        CommandLogger repo = new(CreateTemporaryDatabase(), clock);
        const string userId = "123";
        const string command = "irc line text";
        IImmutableList<string> args = ImmutableList.Create("a", "b", "c");
        const string response = "message text";
        Instant timestamp = Instant.FromUnixTimeSeconds(123);
        clock.GetCurrentInstant().Returns(timestamp);

        // persist to db
        CommandLog written = await repo.Log(userId, command, args, response);
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.Command, Is.EqualTo(command));
        Assert.That(written.Args, Is.EqualTo(args));
        Assert.That(written.Response, Is.EqualTo(response));
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.Id, Is.Not.Null);

        // read from db
        List<CommandLog> allItems = await repo.Collection.Find(FilterDefinition<CommandLog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        CommandLog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.Command, Is.EqualTo(command));
        Assert.That(read.Args, Is.EqualTo(args));
        Assert.That(read.Response, Is.EqualTo(response));
        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
    }

    [Test]
    public async Task BadgeLogRepo()
    {
        BadgeLogRepo repo = new(CreateTemporaryDatabase());
        string badgeId = ObjectId.GenerateNewId().ToString();
        const string badgeLogType = "type";
        const string userId = "user";
        Instant timestamp = Instant.FromUnixTimeSeconds(123);

        // persist to db
        IDictionary<string, object?> data = new Dictionary<string, object?> { ["some"] = "data" };
        BadgeLog written = await repo.Log(badgeId, badgeLogType, userId, timestamp, data);
        Assert.That(written.BadgeId, Is.EqualTo(badgeId));
        Assert.That(written.BadgeLogType, Is.EqualTo(badgeLogType));
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.AdditionalData, Is.EqualTo(data));
        Assert.That(written.Id, Is.Not.Null);

        // read from db
        List<BadgeLog> allItems = await repo.Collection.Find(FilterDefinition<BadgeLog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        BadgeLog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.BadgeId, Is.EqualTo(badgeId));
        Assert.That(read.BadgeLogType, Is.EqualTo(badgeLogType));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
        Assert.That(read.AdditionalData, Is.EqualTo(data));
    }

    [Test]
    public async Task MessagelogRepo()
    {
        MessagelogRepo repo = new(CreateTemporaryDatabase());
        const string userId = "123";
        const string ircLine = "irc line text";
        const string message = "message text";
        Instant timestamp = Instant.FromUnixTimeSeconds(123);

        // persist to db
        Messagelog written = await repo.LogChat(userId, ircLine, message, timestamp);
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.IrcLine, Is.EqualTo(ircLine));
        Assert.That(written.Message, Is.EqualTo(message));
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.Id, Is.Not.Null);

        // read from db
        List<Messagelog> allItems = await repo.Collection.Find(FilterDefinition<Messagelog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        Messagelog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.IrcLine, Is.EqualTo(ircLine));
        Assert.That(read.Message, Is.EqualTo(message));
        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
    }

    [Test]
    public async Task MessagequeueRepo()
    {
        OutgoingMessagequeueRepo repo = new(CreateTemporaryDatabase());
        const string ircLine = "some text";

        // persist to db
        OutgoingMessagequeueItem written = await repo.EnqueueMessage(ircLine);
        Assert.That(written.IrcLine, Is.EqualTo(ircLine));
        Assert.That(written.Id, Is.Not.Null);

        // read from db
        List<OutgoingMessagequeueItem> allItems = await repo.Collection
            .Find(FilterDefinition<OutgoingMessagequeueItem>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        OutgoingMessagequeueItem read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.IrcLine, Is.EqualTo(ircLine));
    }

    [Test]
    public async Task SubscriptionLogRepo()
    {
        SubscriptionLogRepo repo = new(CreateTemporaryDatabase());
        Instant timestamp = Instant.FromUnixTimeSeconds(123);
        const string userId = "123";
        const int monthsStreak = 101;
        const int monthsNumPrev = 103;
        const int monthsNumNew = 105;
        const int monthsDifference = 2;
        const int loyaltyLeaguePrev = 20;
        const int loyaltyLeagueNew = 21;
        const int loyaltyCompletions = 1;
        const int rewardTokens = 10;
        const bool isGift = true;
        const string subMessage = "message text";
        const SubscriptionTier subPlan = SubscriptionTier.Tier2;
        const string subPlanName = "plan name";

        // persist to db
        SubscriptionLog written = await repo.LogSubscription(userId, timestamp,
            monthsStreak, monthsNumPrev, monthsNumNew, monthsDifference,
            loyaltyLeaguePrev, loyaltyLeagueNew, loyaltyCompletions, rewardTokens, isGift,
            subMessage, subPlan, subPlanName);
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.MonthsStreak, Is.EqualTo(monthsStreak));
        Assert.That(written.MonthsNumPrev, Is.EqualTo(monthsNumPrev));
        Assert.That(written.MonthsNumNew, Is.EqualTo(monthsNumNew));
        Assert.That(written.MonthsDifference, Is.EqualTo(monthsDifference));
        Assert.That(written.LoyaltyLeaguePrev, Is.EqualTo(loyaltyLeaguePrev));
        Assert.That(written.LoyaltyLeagueNew, Is.EqualTo(loyaltyLeagueNew));
        Assert.That(written.LoyaltyCompletions, Is.EqualTo(loyaltyCompletions));
        Assert.That(written.RewardTokens, Is.EqualTo(rewardTokens));
        Assert.That(written.IsGift, Is.EqualTo(isGift));
        Assert.That(written.SubMessage, Is.EqualTo(subMessage));
        Assert.That(written.SubPlan, Is.EqualTo(subPlan));
        Assert.That(written.SubPlanName, Is.EqualTo(subPlanName));
        Assert.That(written.Id, Is.Not.Null);

        // read from db
        List<SubscriptionLog> allItems =
            await repo.Collection.Find(FilterDefinition<SubscriptionLog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        SubscriptionLog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));

        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.MonthsStreak, Is.EqualTo(monthsStreak));
        Assert.That(read.MonthsNumPrev, Is.EqualTo(monthsNumPrev));
        Assert.That(read.MonthsNumNew, Is.EqualTo(monthsNumNew));
        Assert.That(read.MonthsDifference, Is.EqualTo(monthsDifference));
        Assert.That(read.LoyaltyLeaguePrev, Is.EqualTo(loyaltyLeaguePrev));
        Assert.That(read.LoyaltyLeagueNew, Is.EqualTo(loyaltyLeagueNew));
        Assert.That(read.LoyaltyCompletions, Is.EqualTo(loyaltyCompletions));
        Assert.That(read.RewardTokens, Is.EqualTo(rewardTokens));
        Assert.That(read.IsGift, Is.EqualTo(isGift));
        Assert.That(read.SubMessage, Is.EqualTo(subMessage));
        Assert.That(read.SubPlan, Is.EqualTo(subPlan));
        Assert.That(read.SubPlanName, Is.EqualTo(subPlanName));
    }

    [Test]
    public async Task InputLogRepo()
    {
        InputLogRepo repo = new(CreateTemporaryDatabase());
        const string userId = "123";
        const string message = "start9";
        Instant timestamp = Instant.FromUnixTimeSeconds(123);

        // persist to db
        InputLog written = await repo.LogInput(userId, message, timestamp);
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.Message, Is.EqualTo(message));
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.Id, Is.Not.Null);

        // read from db
        List<InputLog> allItems = await repo.Collection.Find(FilterDefinition<InputLog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        InputLog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.Message, Is.EqualTo(message));
        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
    }

    [Test]
    public async Task BanLogRepo()
    {
        BanLogRepo repo = new(CreateTemporaryDatabase());
        const string userId = "123";
        const string issuerUserId = "999";
        const string type = "test_ban";
        const string reason = "was very naughty";
        Instant timestamp = Instant.FromUnixTimeSeconds(123);

        // persist to db
        BanLog written = await repo.LogBan(userId, type, reason, issuerUserId, timestamp);
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.Type, Is.EqualTo(type));
        Assert.That(written.Reason, Is.EqualTo(reason));
        Assert.That(written.IssuerUserId, Is.EqualTo(issuerUserId));
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.Id, Is.Not.Null);

        // read from db
        List<BanLog> allItems = await repo.Collection.Find(FilterDefinition<BanLog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        BanLog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.Type, Is.EqualTo(type));
        Assert.That(read.Reason, Is.EqualTo(reason));
        Assert.That(read.IssuerUserId, Is.EqualTo(issuerUserId));
        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
    }

    [Test]
    public async Task TimeoutLogRepo()
    {
        TimeoutLogRepo repo = new(CreateTemporaryDatabase());
        const string userId = "123";
        const string issuerUserId = "999";
        const string type = "test_timeout";
        const string reason = "was only a bit naughty";
        Duration? duration = Duration.FromSeconds(120);
        Instant timestamp = Instant.FromUnixTimeSeconds(123);

        // persist to db
        TimeoutLog written = await repo.LogTimeout(userId, type, reason, issuerUserId, timestamp, duration);
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.Type, Is.EqualTo(type));
        Assert.That(written.Reason, Is.EqualTo(reason));
        Assert.That(written.IssuerUserId, Is.EqualTo(issuerUserId));
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.Duration, Is.EqualTo(duration));
        Assert.That(written.Id, Is.Not.Null);

        // read from db
        List<TimeoutLog> allItems = await repo.Collection.Find(FilterDefinition<TimeoutLog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        TimeoutLog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.Type, Is.EqualTo(type));
        Assert.That(read.Reason, Is.EqualTo(reason));
        Assert.That(read.IssuerUserId, Is.EqualTo(issuerUserId));
        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
        Assert.That(read.Duration, Is.EqualTo(duration));
    }

    [Test]
    public async Task TimeoutLogRepoNullDuration()
    {
        TimeoutLogRepo repo = new(CreateTemporaryDatabase());
        Duration? duration = null;
        Instant timestamp = Instant.FromUnixTimeSeconds(123);

        TimeoutLog written = await repo.LogTimeout("123", "test", "test", "999", timestamp, duration);
        Assert.That(written.Duration, Is.EqualTo(duration));
        List<TimeoutLog> allItems = await repo.Collection.Find(FilterDefinition<TimeoutLog>.Empty).ToListAsync();
        Assert.That(allItems, Has.Count.EqualTo(1));
        Assert.That(allItems[0].Duration, Is.EqualTo(duration));
    }

    [Test]
    public async Task ChattersSnapshotsRepo()
    {
        ChattersSnapshotsRepo repo = new(CreateTemporaryDatabase());
        var usernames = ImmutableList.Create<string>("Karl", "Fritz");
        var userIds = ImmutableList.Create<string>("1234", "5678");
        const string channel = "twitchplayspokemon";
        Instant timestamp = Instant.FromUnixTimeSeconds(123);

        // persist to db
        ChattersSnapshot written = await repo.LogChattersSnapshot(usernames, userIds, channel, timestamp);
        Assert.That(written.ChatterNames, Is.EqualTo(usernames));
        Assert.That(written.ChatterIds, Is.EqualTo(userIds));
        Assert.That(written.Channel, Is.EqualTo(channel));
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.Id, Is.Not.Null);

        // read from db
        List<ChattersSnapshot> allItems = await repo.Collection.Find(FilterDefinition<ChattersSnapshot>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        ChattersSnapshot read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.ChatterNames, Is.EqualTo(usernames));
        Assert.That(read.ChatterIds, Is.EqualTo(userIds));
        Assert.That(read.Channel, Is.EqualTo(channel));
        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
    }
}
