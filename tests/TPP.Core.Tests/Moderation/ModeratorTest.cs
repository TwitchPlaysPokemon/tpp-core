using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
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
        var executor = new Mock<IExecutor>();
        var modLogRepo = new Mock<IModLogRepo>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0));
        IImmutableList<IModerationRule> rules = ImmutableList.Create<IModerationRule>(new GivePointsRule(50));
        var moderator = new Moderator(
            NullLogger<Moderator>.Instance, executor.Object, rules, modLogRepo.Object, clock.Object,
            pointsForTimeout: 100);

        Expression<Func<IExecutor, Task>> invocation =
            e => e.Timeout(user, It.IsAny<string>(), Duration.FromMinutes(2));

        const string msg1 = "not enough points yet";
        Assert.IsTrue(await moderator.Check(new Message(user, msg1, MessageSource.Chat, string.Empty)));
        executor.Verify(invocation, Times.Never);

        const string msg2 = "enough points for timeout";
        Assert.IsFalse(await moderator.Check(new Message(user, msg2, MessageSource.Chat, string.Empty)));
        executor.Verify(invocation, Times.Once);

        const string msg3 = "points reset after timeout, no additional timeout yet";
        Assert.IsTrue(await moderator.Check(new Message(user, msg3, MessageSource.Chat, string.Empty)));
        executor.Verify(invocation, Times.Once);

        const string msg4 = "timeout again after points were reached a second time";
        Assert.IsFalse(await moderator.Check(new Message(user, msg4, MessageSource.Chat, string.Empty)));
        executor.Verify(invocation, Times.Exactly(2));
    }

    [Test]
    public async Task points_decay_over_time()
    {
        User user = MockUser("MockUser");
        var executor = new Mock<IExecutor>();
        var modLogRepo = new Mock<IModLogRepo>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0));
        IImmutableList<IModerationRule> rules = ImmutableList.Create<IModerationRule>(new GivePointsRule(50));
        var moderator = new Moderator(
            NullLogger<Moderator>.Instance, executor.Object, rules, modLogRepo.Object, clock.Object,
            pointsDecayPerSecond: 1, pointsForTimeout: 100);

        const string msg1 = "not enough points yet";
        Assert.IsTrue(await moderator.Check(new Message(user, msg1, MessageSource.Chat, string.Empty)));

        const string msg2 = "some time passed, so still not enough points for a timeout (2*50 - 1 = 99)";
        clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(1));
        Assert.IsTrue(await moderator.Check(new Message(user, msg2, MessageSource.Chat, string.Empty)));

        const string msg3 = "some more time passed, still barely not enough for a timeout (3*50 - 51 = 99)";
        clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(51));
        Assert.IsTrue(await moderator.Check(new Message(user, msg3, MessageSource.Chat, string.Empty)));

        const string msg4 = "a little more time passed, not enough points decayed (4*50 - 99 = 101) " +
                            "and therefore a timeout is issued";
        clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(99));
        Assert.IsFalse(await moderator.Check(new Message(user, msg4, MessageSource.Chat, string.Empty)));
        const string reasons = "points for testing #1, points for testing #2, " +
                               "points for testing #3 and points for testing #4";
        executor.Verify(e => e.Timeout(user, reasons, Duration.FromMinutes(2)), Times.Once);
    }

    [Test]
    public async Task points_below_minimum_dont_count()
    {
        User user = MockUser("MockUser");
        var executor = new Mock<IExecutor>();
        var modLogRepo = new Mock<IModLogRepo>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0));
        IImmutableList<IModerationRule> rules = ImmutableList.Create<IModerationRule>(new GivePointsRule(50));
        var moderator = new Moderator(
            NullLogger<Moderator>.Instance, executor.Object, rules, modLogRepo.Object, clock.Object,
            minPoints: 51, pointsForTimeout: 100);

        Assert.IsTrue(await moderator.Check(new Message(user, "no points",
            MessageSource.Chat, string.Empty)));
        Assert.IsTrue(await moderator.Check(new Message(user, "still no points",
            MessageSource.Chat, string.Empty)));
        Assert.IsTrue(await moderator.Check(new Message(user, "everything is fine",
            MessageSource.Chat, string.Empty)));
    }

    [Test]
    public async Task points_timeout_includes_recent_reasons()
    {
        User user = MockUser("MockUser");
        var executor = new Mock<IExecutor>();
        var modLogRepo = new Mock<IModLogRepo>();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(0));
        IImmutableList<IModerationRule> rules = ImmutableList.Create<IModerationRule>(new GivePointsRule(50));
        var moderator = new Moderator(
            NullLogger<Moderator>.Instance, executor.Object, rules, modLogRepo.Object, clock.Object,
            pointsDecayPerSecond: 1, pointsForTimeout: 100);

        const string msg1 = "not enough points";
        Assert.IsTrue(await moderator.Check(new Message(user, msg1, MessageSource.Chat, string.Empty)));

        const string msg2 = "first violation's points completely decayed, but not enough points for a timeout";
        clock.Setup(c => c.GetCurrentInstant()).Returns(Instant.FromUnixTimeSeconds(50));
        Assert.IsTrue(await moderator.Check(new Message(user, msg2, MessageSource.Chat, string.Empty)));
        const string msg4 = "enough points for a timeout now";
        Assert.IsFalse(await moderator.Check(new Message(user, msg4, MessageSource.Chat, string.Empty)));
        // listed reasons do not include the first completely decayed violation
        const string reasons = "points for testing #2 and points for testing #3";
        executor.Verify(e => e.Timeout(user, reasons, Duration.FromMinutes(2)), Times.Once);
    }
}
