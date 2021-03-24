using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Repos;
using TPP.Persistence.Repos;

namespace TPP.Core
{
    public class Transmutation
    {
        private const double PiOver2 = Math.PI / 2d;
        private static readonly double Ln2 = Math.Log(2);

        private readonly IBadgeStatsRepo _badgeStatsRepo;
        private readonly Func<double> _random;
        private readonly ImmutableSortedSet<PkmnSpecies> _transmutableBadges;

        public Transmutation(
            IBadgeStatsRepo badgeStatsRepo,
            Func<double> random,
            ImmutableSortedSet<PkmnSpecies> transmutableBadges)
        {
            _badgeStatsRepo = badgeStatsRepo;
            _random = random;
            _transmutableBadges = transmutableBadges;
            if (transmutableBadges.IsEmpty)
                throw new ArgumentException("must provide at least 1 transmutable", nameof(transmutableBadges));
        }

        private static double CombineRarities(IEnumerable<double> rarities) =>
            1d - Math.Atan(rarities.Select(x => Math.Tan(PiOver2 * (1d - x))).Sum()) / PiOver2;

        private double RandomizeRarity(double rarity, double total)
        {
            if (rarity == 0 || total == 0) return rarity;
            double x = _random();
            double product = rarity * total;
            double productlog = Math.Log(product);
            double parameter = (Math.Log(1 + product) + productlog - Ln2) / (rarity * productlog);
            if (x <= 0.5d)
            {
                return Math.Pow(rarity * (2 * x), 1d / (rarity * parameter - 1d));
            }
            else
            {
                double invRarity = 1d - rarity;
                return 1d - Math.Pow(invRarity * (2d - 2d * x), 1d / (invRarity * parameter - 1d));
            }
        }

        private double GetTransmutedRarity(IEnumerable<double> rarities, double total)
        {
            double combined = CombineRarities(rarities);
            double randomized = RandomizeRarity(combined, total);
            return Math.Clamp(randomized, 0d, 1d);
        }

        private PkmnSpecies GetBadgeForRarity(double rarity, IDictionary<PkmnSpecies, double> rarities)
        {
            ((PkmnSpecies, double)? closestNeg, (PkmnSpecies, double)? closestPos) = (null, null);

            foreach ((PkmnSpecies species, double r) in rarities.OrderBy(kvp => (kvp.Value, _random())))
            {
                if (r < rarity)
                {
                    closestNeg = (species, r);
                }
                else
                {
                    closestPos = (species, r);
                    break;
                }
            }
            if (closestNeg == null || closestPos == null)
                return closestNeg?.Item1 ?? closestPos?.Item1
                    ?? throw new ArgumentException("must provide at least 1 possible result rarity", nameof(rarities));
            double min = closestNeg.Value.Item2;
            double max = closestPos.Value.Item2;
            double randomRounding = min + _random() * (max - min);
            return randomRounding < rarity ? closestNeg.Value.Item1 : closestPos.Value.Item1;
        }

        public async Task<PkmnSpecies> Transmute(IImmutableSet<PkmnSpecies> inputSpecies)
        {
            IDictionary<PkmnSpecies, BadgeStat> stats = await _badgeStatsRepo.GetBadgeStats();
            ImmutableSortedSet<PkmnSpecies> transmutables = _transmutableBadges.Except(inputSpecies);
            if (transmutables.IsEmpty)
            {
                throw new ArgumentException(
                    "there are no transmutables left after removing all input species from the pool",
                    nameof(inputSpecies)); // TODO
            }

            double totalExisting = transmutables.Select(t => stats[t].RarityCount).Sum();
            double totalGenerated = transmutables.Select(t => stats[t].RarityCountGenerated).Sum();
            var rarities = transmutables.ToImmutableSortedDictionary(t => t, t => stats[t].Rarity);

            const double factor = BadgeRepo.CountExistingFactor;
            double total = 1d / ((1d - factor) / totalGenerated + factor / totalExisting);

            double transmutedRarity = GetTransmutedRarity(inputSpecies.Select(s => stats[s].Rarity), total);
            return GetBadgeForRarity(transmutedRarity, rarities);
        }
    }
}
