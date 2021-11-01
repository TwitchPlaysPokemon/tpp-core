using System.Collections.Generic;
using System.Linq;
using NodaTime;
using TPP.Model;

namespace TPP.Core.Commands
{
    public class GlobalCooldown
    {
        private readonly IClock _clock;
        private readonly Duration _duration;
        private Instant _lastExecution = Instant.MinValue;

        public GlobalCooldown(IClock clock, Duration duration)
        {
            _clock = clock;
            _duration = duration;
        }

        /// Checks whether the cooldown has lapsed.
        /// If so, returns true and resets the cooldown.
        public bool CheckLapsedThenReset()
        {
            Instant now = _clock.GetCurrentInstant();
            bool isOnCooldown = _lastExecution + _duration > now;
            if (isOnCooldown) return false;
            _lastExecution = now;
            return true;
        }
    }

    public class PerUserCooldown
    {
        private readonly IClock _clock;
        private readonly Duration _duration;
        private Dictionary<User, Instant> _lastExecutions = new Dictionary<User, Instant>();

        public PerUserCooldown(IClock clock, Duration duration)
        {
            _clock = clock;
            _duration = duration;
        }

        private void PruneLapsed(Instant now)
        {
            _lastExecutions = _lastExecutions
                .Where(kvp => kvp.Value + _duration > now)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// Checks whether the cooldown has lapsed.
        public bool CheckLapsed(User user)
        {
            PruneLapsed(_clock.GetCurrentInstant());
            return !_lastExecutions.ContainsKey(user);
        }

        /// Resets the cooldown.
        public void Reset(User user)
        {
            _lastExecutions[user] = _clock.GetCurrentInstant();
        }

        /// Checks whether the cooldown has lapsed.
        /// If so, returns true and resets the cooldown.
        public bool CheckLapsedThenReset(User user)
        {
            Instant now = _clock.GetCurrentInstant();
            PruneLapsed(now);
            bool isOnCooldown = _lastExecutions.ContainsKey(user);
            if (isOnCooldown) return false;
            _lastExecutions[user] = now;
            return true;
        }
    }
}
