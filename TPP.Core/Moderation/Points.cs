using System;
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
            PruneDecayed();
            _points.Add(new GivenPoints(points, reason, _clock.GetCurrentInstant()));
        }

        public bool IsEmpty()
        {
            PruneDecayed();
            return _points.Count == 0;
        }

        public int GetCurrentPoints()
        {
            if (_points.Count == 0) return 0;

            Instant now = _clock.GetCurrentInstant();
            Instant decayingSince = _points[0].GivenAt;
            double pointsDecayed = (now - decayingSince).TotalSeconds * _decayPerSecond;
            int pointsMaybeNegative = (int)(_points.Sum(p => p.Points) - pointsDecayed);
            return Math.Max(0, pointsMaybeNegative);
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
            if (GetCurrentPoints() == 0) _points.Clear();
        }
    }
}
