using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.Core.Tests
{
    public class TransmutationTest
    {
        [Test]
        public async Task Test()
        {
            Mock<IBadgeStatsRepo> badgeStatsRepo = new();
            // PkmnSpecies species1 = PkmnSpecies.OfId("1");
            // PkmnSpecies species2 = PkmnSpecies.OfId("2");
            // PkmnSpecies species3 = PkmnSpecies.OfId("3");
            // PkmnSpecies species4 = PkmnSpecies.OfId("4");
            // PkmnSpecies species5 = PkmnSpecies.OfId("5");
            // // distribution: 500x#1 400x#2 97x#3 2x#4 1x#5 (total 1000)
            // var stats = new Dictionary<PkmnSpecies, BadgeStat>
            // {
            //     [species1] = new(species1, 500, 500, 500, 500, 500/1000d),
            //     [species2] = new(species2, 400, 400, 400, 400, 400/1000d),
            //     [species3] = new(species3, 97, 97, 97, 97, 97/1000d),
            //     [species4] = new(species4, 2, 2, 2, 2, 2/1000d),
            //     [species5] = new(species5, 1, 1, 1, 1, 1/1000d),
            // }.ToImmutableSortedDictionary();
            PkmnSpecies species1 = PkmnSpecies.OfId("1");
            PkmnSpecies species2 = PkmnSpecies.OfId("2");
            PkmnSpecies species3 = PkmnSpecies.OfId("3");
            PkmnSpecies species4 = PkmnSpecies.OfId("4");
            PkmnSpecies species5 = PkmnSpecies.OfId("5");
            // distribution: 500x#1 400x#2 97x#3 2x#4 1x#5 (total 1000)
            var stats = new Dictionary<PkmnSpecies, BadgeStat>
            {
                [species1] = new(species1, 10, 10, 10, 10, 10/20d),
                [species2] = new(species2, 6, 6, 6, 6, 6/20d),
                [species3] = new(species3, 3, 3, 3, 3, 3/20d),
                [species4] = new(species4, 1, 1, 1, 1, 1/20d),
            }.ToImmutableSortedDictionary();
            Assert.AreEqual(1d, stats.Values.Select(r => r.Rarity).Sum());
            badgeStatsRepo.Setup(r => r.GetBadgeStats()).ReturnsAsync(stats);

            var transmutables = ImmutableSortedSet.Create(species1, species2, species3, species4);

            const int num = 1_000_000;
            List<PkmnSpecies> results = new();
            var random = new Random();
            for (int i = 0; i < num; i++)
            {
                // double Random() => i / (double)num;
                double Random() => random.NextDouble();
                var transmutation = new Transmutation(badgeStatsRepo.Object, Random, transmutables);
                var transmuteInputs = ImmutableHashSet.Create(species1, species1, species1);
                PkmnSpecies result = await transmutation.Transmute(transmuteInputs);
                results.Add(result);
            }

            ImmutableSortedDictionary<PkmnSpecies, int> counts = results
                .GroupBy(s => s).ToImmutableSortedDictionary(grp => grp.Key, grp => grp.Count());
            Console.WriteLine(string.Join(", ", counts.Select(kvp => $"{kvp.Value}x{kvp.Key}")));
        }
    }
}
