using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NodaTime;

namespace TPP.Core.Moderation
{
    internal record GivenPoints(int Points, string Reason, Instant GivenAt);

    public class PointStore
    {
        private readonly IClock _clock;
        private readonly float _decayPerSecond;
        private readonly List<GivenPoints> _points;

        public PointStore(IClock clock, float decayPerSecond)
        {
            _clock = clock;
            _decayPerSecond = decayPerSecond;
            _points = new List<GivenPoints>();
        }

        public void AddPoints(int points, string reason)
        {
            _points.Add(new GivenPoints(points, reason, _clock.GetCurrentInstant()));
        }

        public bool IsEmpty()
        {
            PruneDecayed();
            return _points.Count == 0;
        }

        public int GetCurrentPoints()
        {
            PruneDecayed();
            if (_points.Count == 0) return 0;

            Instant now = _clock.GetCurrentInstant();
            Instant decayingSince = _points[0].GivenAt;
            double pointsDecayed = (now - decayingSince).TotalSeconds * _decayPerSecond;
            return (int)(_points.Sum(p => p.Points) - pointsDecayed);
        }

        public IImmutableList<(int, string)> GetTopViolations()
        {
            PruneDecayed();
            return _points
                .Select(p => (p.Points, p.Reason))
                .GroupBy(tpl => tpl.Item2)
                .Select(group => (group.Sum(tpl => tpl.Item1), group.Key))
                .OrderBy(tpl => -tpl.Item1)
                .ToImmutableList();
        }

        private void PruneDecayed()
        {
            if (_points.Count == 0) return;

            int firstNonDecayedIndex = 0;
            Instant expiresAt = _points[0].GivenAt;
            for (int i = 0; i < _points.Count; i++)
            {
                Instant nextStart = i + 1 < _points.Count
                    ? _points[i + 1].GivenAt
                    : _clock.GetCurrentInstant();
                Duration expireDuration = Duration.FromSeconds(_points[i].Points / (double)_decayPerSecond);
                expiresAt += expireDuration;
                if (nextStart >= expiresAt)
                {
                    firstNonDecayedIndex = i + 1;
                    expiresAt = nextStart;
                }
            }
            _points.RemoveRange(0, firstNonDecayedIndex);
        }
    }
}
