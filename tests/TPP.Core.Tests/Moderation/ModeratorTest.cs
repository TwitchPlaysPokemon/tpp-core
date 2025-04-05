using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using TPP.Core.Moderation;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Tests.Moderation;

internal class GivePointsRule : IModerationRule
{
    public string Id => "test-give-points";
    private readonly int _points;
    private int _rollingCount = 1;
    public GivePointsRule(int points) => _points = points;

    public RuleResult Check(Message message) =>
        new RuleResult.GivePoints(_points, $"points for testing #{_rollingCount++}");
}

public class ModeratorTest
{
    private static User MockUser(string name) => new(
        id: Guid.NewGuid().ToString(),
        name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
        firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
        lastMessageAt: null, pokeyen: 0, tokens: 0);

    [Test]
    public async Task timeout_after_too_many_points()
    {
        User user = MockUser("MockUser");
        var executor = Substitute.For<IExecutor>();
        var modbotLogRepo = Substitute.For<IModbotLogRepo>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(0));
        IImmutableList<IModerationRule> rules = ImmutableList.Create<IModerationRule>(new GivePointsRule(50));
        var moderator = new Moderator(
            NullLogger<Moderator>.Instance, executor, rules, modbotLogRepo, clock,
            pointsForTimeout: 100);

        const string msg1 = "not enough points yet";
        Assert.That(await moderator.Check(new Message(user, msg1, new MessageSource.PrimaryChat(), string.Empty)), Is.True);
        await executor.DidNotReceive().Timeout(user, Arg.Any<string>(), Duration.FromMinutes(2));

        const string msg2 = "enough points for timeout";
        Assert.That(await moderator.Check(new Message(user, msg2, new MessageSource.PrimaryChat(), string.Empty)), Is.False);
        await executor.Received(1).Timeout(user, Arg.Any<string>(), Duration.FromMinutes(2));

        const string msg3 = "points reset after timeout, no additional timeout yet";
        Assert.That(await moderator.Check(new Message(user, msg3, new MessageSource.PrimaryChat(), string.Empty)), Is.True);
        await executor.Received(1).Timeout(user, Arg.Any<string>(), Duration.FromMinutes(2));

        const string msg4 = "timeout again after points were reached a second time";
        Assert.That(await moderator.Check(new Message(user, msg4, new MessageSource.PrimaryChat(), string.Empty)), Is.False);
        await executor.Received(2).Timeout(user, Arg.Any<string>(), Duration.FromMinutes(2));
    }

    [Test]
    public async Task points_decay_over_time()
    {
        User user = MockUser("MockUser");
        var executor = Substitute.For<IExecutor>();
        var modbotLogRepo = Substitute.For<IModbotLogRepo>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(0));
        IImmutableList<IModerationRule> rules = ImmutableList.Create<IModerationRule>(new GivePointsRule(50));
        var moderator = new Moderator(
            NullLogger<Moderator>.Instance, executor, rules, modbotLogRepo, clock,
            pointsDecayPerSecond: 1, pointsForTimeout: 100);

        const string msg1 = "not enough points yet";
        Assert.That(await moderator.Check(new Message(user, msg1, new MessageSource.PrimaryChat(), string.Empty)), Is.True);

        const string msg2 = "some time passed, so still not enough points for a timeout (2*50 - 1 = 99)";
        clock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(1));
        Assert.That(await moderator.Check(new Message(user, msg2, new MessageSource.PrimaryChat(), string.Empty)), Is.True);

        const string msg3 = "some more time passed, still barely not enough for a timeout (3*50 - 51 = 99)";
        clock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(51));
        Assert.That(await moderator.Check(new Message(user, msg3, new MessageSource.PrimaryChat(), string.Empty)), Is.True);

        const string msg4 = "a little more time passed, not enough points decayed (4*50 - 99 = 101) " +
                            "and therefore a timeout is issued";
        clock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(99));
        Assert.That(await moderator.Check(new Message(user, msg4, new MessageSource.PrimaryChat(), string.Empty)), Is.False);
        const string reasons = "points for testing #1, points for testing #2, " +
                               "points for testing #3 and points for testing #4";
        await executor.Received(1).Timeout(user, reasons, Duration.FromMinutes(2));
    }

    [Test]
    public async Task points_below_minimum_dont_count()
    {
        User user = MockUser("MockUser");
        var executor = Substitute.For<IExecutor>();
        var modbotLogRepo = Substitute.For<IModbotLogRepo>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(0));
        IImmutableList<IModerationRule> rules = ImmutableList.Create<IModerationRule>(new GivePointsRule(50));
        var moderator = new Moderator(
            NullLogger<Moderator>.Instance, executor, rules, modbotLogRepo, clock,
            minPoints: 51, pointsForTimeout: 100);

        Assert.That(await moderator.Check(new Message(user, "no points",
            new MessageSource.PrimaryChat(), string.Empty)), Is.True);
        Assert.That(await moderator.Check(new Message(user, "still no points",
            new MessageSource.PrimaryChat(), string.Empty)), Is.True);
        Assert.That(await moderator.Check(new Message(user, "everything is fine",
            new MessageSource.PrimaryChat(), string.Empty)), Is.True);
    }

    [Test]
    public async Task points_timeout_includes_recent_reasons()
    {
        User user = MockUser("MockUser");
        var executor = Substitute.For<IExecutor>();
        var modbotLogRepo = Substitute.For<IModbotLogRepo>();
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(0));
        IImmutableList<IModerationRule> rules = ImmutableList.Create<IModerationRule>(new GivePointsRule(50));
        var moderator = new Moderator(
            NullLogger<Moderator>.Instance, executor, rules, modbotLogRepo, clock,
            pointsDecayPerSecond: 1, pointsForTimeout: 100);

        const string msg1 = "not enough points";
        Assert.That(await moderator.Check(new Message(user, msg1, new MessageSource.PrimaryChat(), string.Empty)), Is.True);

        const string msg2 = "first violation's points completely decayed, but not enough points for a timeout";
        clock.GetCurrentInstant().Returns(Instant.FromUnixTimeSeconds(50));
        Assert.That(await moderator.Check(new Message(user, msg2, new MessageSource.PrimaryChat(), string.Empty)), Is.True);
        const string msg4 = "enough points for a timeout now";
        Assert.That(await moderator.Check(new Message(user, msg4, new MessageSource.PrimaryChat(), string.Empty)), Is.False);
        // listed reasons do not include the first completely decayed violation
        const string reasons = "points for testing #2 and points for testing #3";
        await executor.Received(1).Timeout(user, reasons, Duration.FromMinutes(2));
    }
}
