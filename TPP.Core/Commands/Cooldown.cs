using System.Collections.Generic;
using System.Linq;
using NodaTime;
using TPP.Model;

namespace TPP.Core.Commands;

public class GlobalCooldown(IClock clock, Duration duration)
{
    private Instant _lastExecution = Instant.MinValue;

    /// Checks whether the cooldown has lapsed.
    /// If so, returns true and resets the cooldown.
    public bool CheckLapsedThenReset()
    {
        Instant now = clock.GetCurrentInstant();
        bool isOnCooldown = _lastExecution + duration > now;
        if (isOnCooldown) return false;
        _lastExecution = now;
        return true;
    }
}

public class PerUserCooldown(IClock clock, Duration duration)
{
    public Duration Duration { get; } = duration;
    private Dictionary<User, Instant> _lastExecutions = new();

    private void PruneLapsed(Instant now)
    {
        _lastExecutions = _lastExecutions
            .Where(kvp => kvp.Value + Duration > now)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// Checks whether the cooldown has lapsed.
    public bool CheckLapsed(User user)
    {
        PruneLapsed(clock.GetCurrentInstant());
        return !_lastExecutions.ContainsKey(user);
    }

    /// Resets the cooldown.
    public void Reset(User user)
    {
        _lastExecutions[user] = clock.GetCurrentInstant();
    }

    /// Checks whether the cooldown has lapsed.
    /// If so, returns true and resets the cooldown.
    public bool CheckLapsedThenReset(User user)
    {
        Instant now = clock.GetCurrentInstant();
        PruneLapsed(now);
        bool isOnCooldown = _lastExecutions.ContainsKey(user);
        if (isOnCooldown) return false;
        _lastExecutions[user] = now;
        return true;
    }
}
