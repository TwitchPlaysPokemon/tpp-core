using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Moderation;

public interface IModerator
{
    /// Checks a message and may perform some punitive actions,
    /// returning whether the message was okay.
    Task<bool> Check(Message message);
}

public class Moderator(
    ILogger<Moderator> logger,
    IExecutor executor,
    IImmutableList<IModerationRule> rules,
    IModbotLogRepo modbotLogRepo,
    IClock clock,
    int freeTimeouts = 2,
    float pointsDecayPerSecond = 1f,
    int minPoints = 20,
    int pointsForTimeout = 300,
    int pointsForDelete = 200)
    : IModerator
{
    private static readonly Duration RecentTimeoutsLimit = Duration.FromDays(7);
    private static readonly Duration InitialTimeoutDuration = Duration.FromMinutes(2);
    // twitch does not allow timeouts beyond 2 weeks
    private static readonly Duration MaxTimeoutDuration = Duration.FromDays(14) - Duration.FromSeconds(1);

    private readonly Dictionary<User, PointStore> _pointsPerUser = new();

    private RuleResult ApplyPoints(User user, int points, string reason)
    {
        if (points < minPoints)
        {
            logger.LogDebug($"Ignoring {points} being issued to {user} for reason '{reason}', " +
                             $"because the minimum amount of issuable points is {minPoints}.");
            return new RuleResult.Nothing();
        }

        // clean up expired entries, so we don't leak memory
        List<User> expiredEntries = _pointsPerUser
            .Where(kvp => kvp.Value.IsEmpty())
            .Select(kvp => kvp.Key)
            .ToList();
        expiredEntries.ForEach(u => _pointsPerUser.Remove(u));

        if (!_pointsPerUser.TryGetValue(user, out PointStore? store))
        {
            store = new PointStore(clock, pointsDecayPerSecond);
            _pointsPerUser[user] = store;
        }

        store.AddPoints(points, reason);
        int currentPoints = store.GetCurrentPoints();
        logger.LogDebug($"Issued {points} points to {user} for reason '{reason}', " +
                         $"which now has {currentPoints} total.");

        if (currentPoints >= pointsForTimeout)
        {
            IImmutableList<PointStore.Violation> violations = store.GetTopViolations();
            _pointsPerUser.Remove(user);
            string topReasons;
            if (violations.Count > 1)
                topReasons = string.Join(", ", violations.SkipLast(1).Select(v => v.Reason))
                             + " and " + violations[^1].Reason;
            else
                topReasons = violations[0].Reason;
            return new RuleResult.Timeout(topReasons);
        }
        else if (currentPoints >= pointsForDelete)
        {
            return new RuleResult.DeleteMessage();
        }

        return new RuleResult.Nothing();
    }

    public async Task<bool> Check(Message message)
    {
        bool deleteMessage = false;
        (RuleResult.Timeout, IModerationRule)? timeoutAndRule = null;

        List<(RuleResult, IModerationRule)> pointResults = [];

        void ProcessResult(RuleResult result, IModerationRule rule)
        {
            if (result is RuleResult.GivePoints givePoints)
                pointResults.Add((ApplyPoints(message.User, givePoints.Points, givePoints.Reason), rule));
            else if (result is RuleResult.DeleteMessage)
                deleteMessage = true;
            else if (result is RuleResult.Timeout resultTimeout)
                timeoutAndRule = (resultTimeout, rule);
            else if (result is RuleResult.Nothing) { }
            else
                logger.LogWarning($"unhandled moderator rule result type '{result.GetType()}'");
        }

        foreach (IModerationRule? rule in rules)
        {
            RuleResult result = rule.Check(message);
            ProcessResult(result, rule);
        }
        while (pointResults.Any())
        {
            (RuleResult result, IModerationRule rule) = pointResults.First();
            pointResults.RemoveAt(0);
            ProcessResult(result, rule);
        }

        if (timeoutAndRule.HasValue)
        {
            (RuleResult.Timeout timeout, IModerationRule rule) = timeoutAndRule.Value;
            Duration timeoutDuration = await CalculateTimeoutDuration(message.User);
            await executor.Timeout(message.User, timeout.Message, timeoutDuration);
            await modbotLogRepo.LogAction(message.User, timeout.Message, rule.Id, clock.GetCurrentInstant());
            return false;
        }
        else if (deleteMessage)
        {
            if (message.Details.MessageId != null)
                await executor.DeleteMessage(message.Details.MessageId);
            else
                // Regular messages should always have an id. Whispers don't, but shouldn't be checked by modbot.
                logger.LogWarning($"Modbot cannot delete message because it's missing a message id: {message}");
            return false;
        }
        return true;
    }

    private async Task<Duration> CalculateTimeoutDuration(User user)
    {
        Instant cutoff = clock.GetCurrentInstant() - RecentTimeoutsLimit;
        long recentBans = await modbotLogRepo.CountRecentBans(user, cutoff);

        Duration duration = InitialTimeoutDuration;
        long increases = Math.Max(0, recentBans - freeTimeouts);
        duration *= increases + 1;
        if (duration > MaxTimeoutDuration) duration = MaxTimeoutDuration;

        return duration;
    }
}
