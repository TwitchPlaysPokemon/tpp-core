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
        private readonly SortedList<Instant, GivenPoints> _points;

        public PointStore(IClock clock, float decayPerSecond)
        {
            _clock = clock;
            _decayPerSecond = decayPerSecond;
            _points = new SortedList<Instant, GivenPoints>();
        }

        public void AddPoints(int points, string reason)
        {
            Instant now = _clock.GetCurrentInstant();
            Instant expiresAt = now + Duration.FromSeconds(points / _decayPerSecond);
            while (_points.ContainsKey(expiresAt))
                expiresAt = expiresAt.Plus(Duration.FromMilliseconds(1));
            _points.Add(expiresAt, new GivenPoints(points, reason, now));
        }

        public bool IsEmpty()
        {
            PruneDecayed();
            return _points.Count == 0;
        }

        public int GetCurrentPoints()
        {
            PruneDecayed();
            Instant now = _clock.GetCurrentInstant();
            double totalPoints = _points.Values.Select(p =>
            {
                double pointsDecayed = (now - p.GivenAt).TotalSeconds * _decayPerSecond;
                return p.Points - pointsDecayed;
            }).Sum();
            return (int)totalPoints;
        }

        public IImmutableList<(int, string)> GetTopViolations()
        {
            PruneDecayed();
            Instant now = _clock.GetCurrentInstant();
            return _points.Values
                .Select(p =>
                {
                    double pointsDecayed = (now - p.GivenAt).TotalSeconds * _decayPerSecond;
                    return ((int)(p.Points - pointsDecayed), p.Reason);
                })
                .GroupBy(tpl => tpl.Item2)
                .Select(group => (group.Sum(tpl => tpl.Item1), group.Key))
                .OrderBy(tpl => -tpl.Item1)
                .ToImmutableList();
        }

        private void PruneDecayed()
        {
            Instant now = _clock.GetCurrentInstant();
            while (_points.Count > 0 && _points.Keys.First() < now)
            {
                _points.RemoveAt(0);
            }
        }
    }
}
