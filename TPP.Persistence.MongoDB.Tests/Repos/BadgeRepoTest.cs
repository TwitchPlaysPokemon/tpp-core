using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Repos;
using TPP.Persistence.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos
{
    [Parallelizable(ParallelScope.All)]
    public class BadgeRepoTest : MongoTestBase
    {
        public BadgeRepo CreateBadgeRepo() =>
            new BadgeRepo(CreateTemporaryDatabase(), Mock.Of<IMongoBadgeLogRepo>(), Mock.Of<IClock>());

        [Test]
        public async Task insert_then_read_are_equal()
        {
            BadgeRepo badgeRepo = CreateBadgeRepo();
            // when
            Badge badge = await badgeRepo.AddBadge(null, PkmnSpecies.OfId("16"), Badge.BadgeSource.ManualCreation);

            // then
            Assert.AreNotEqual(string.Empty, badge.Id);
            Badge badgeFromDatabase = await badgeRepo.Collection.Find(b => b.Id == badge.Id).FirstOrDefaultAsync();
            Assert.NotNull(badgeFromDatabase);
            Assert.AreNotSame(badgeFromDatabase, badge);
            Assert.AreEqual(badgeFromDatabase, badge);
        }

        [Test]
        public async Task insert_sets_current_timestamp_as_creation_date()
        {
            Mock<IClock> clockMock = new();
            Instant createdAt = Instant.FromUnixTimeSeconds(123);
            clockMock.Setup(c => c.GetCurrentInstant()).Returns(createdAt);
            IBadgeRepo badgeRepo = new BadgeRepo(
                CreateTemporaryDatabase(), Mock.Of<IMongoBadgeLogRepo>(), clockMock.Object);

            Badge badge = await badgeRepo.AddBadge(null, PkmnSpecies.OfId("16"), Badge.BadgeSource.ManualCreation);
            Assert.AreEqual(createdAt, badge.CreatedAt);
        }

        /// <summary>
        /// Tests that the data gets represented in the database the desired way.
        /// This ensures backwards compatibility to any existing data.
        /// </summary>
        [Test]
        public async Task has_expected_bson_datatypes()
        {
            BadgeRepo badgeRepo = CreateBadgeRepo();
            // when
            PkmnSpecies randomSpecies = PkmnSpecies.OfId("9001");
            Badge badge = await badgeRepo.AddBadge(null, randomSpecies, Badge.BadgeSource.RunCaught);

            // then
            IMongoCollection<BsonDocument> badgesCollectionBson =
                badgeRepo.Collection.Database.GetCollection<BsonDocument>("badges");
            BsonDocument badgeBson = await badgesCollectionBson.Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();
            Assert.AreEqual(BsonObjectId.Create(ObjectId.Parse(badge.Id)), badgeBson["_id"]);
            Assert.AreEqual(BsonNull.Value, badgeBson["user"]);
            Assert.AreEqual(BsonString.Create(randomSpecies.Id), badgeBson["species"]);
            Assert.AreEqual(BsonString.Create("run_caught"), badgeBson["source"]);
        }

        [Test]
        public async Task can_find_by_user()
        {
            IBadgeRepo badgeRepo = CreateBadgeRepo();
            // given
            Badge badgeUserA1 = await badgeRepo.AddBadge("userA", PkmnSpecies.OfId("1"), Badge.BadgeSource.Pinball);
            Badge badgeUserA2 = await badgeRepo.AddBadge("userA", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball);
            Badge badgeUserB = await badgeRepo.AddBadge("userB", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            Badge badgeNobody = await badgeRepo.AddBadge(null, PkmnSpecies.OfId("4"), Badge.BadgeSource.Pinball);

            // when
            List<Badge> resultUserA = await badgeRepo.FindByUser("userA");
            List<Badge> resultUserB = await badgeRepo.FindByUser("userB");
            List<Badge> resultNobody = await badgeRepo.FindByUser(null);

            // then
            Assert.AreEqual(new List<Badge> { badgeUserA1, badgeUserA2 }, resultUserA);
            Assert.AreEqual(new List<Badge> { badgeUserB }, resultUserB);
            Assert.AreEqual(new List<Badge> { badgeNobody }, resultNobody);
        }

        [Test]
        public async Task can_count_by_user_and_species()
        {
            IBadgeRepo badgeRepo = CreateBadgeRepo();
            // given
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("1"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);

            // when
            long countHasNone = await badgeRepo.CountByUserAndSpecies("user", PkmnSpecies.OfId("1"));
            long countHasOne = await badgeRepo.CountByUserAndSpecies("user", PkmnSpecies.OfId("2"));
            long countHasThree = await badgeRepo.CountByUserAndSpecies("user", PkmnSpecies.OfId("3"));

            // then
            Assert.AreEqual(0, countHasNone);
            Assert.AreEqual(1, countHasOne);
            Assert.AreEqual(3, countHasThree);
        }

        [Test]
        public async Task can_count_per_species_for_one_user()
        {
            IBadgeRepo badgeRepo = CreateBadgeRepo();
            // given
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("1"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);

            // when
            ImmutableSortedDictionary<PkmnSpecies, int> result = await badgeRepo.CountByUserPerSpecies("user");

            // then
            ImmutableSortedDictionary<PkmnSpecies, int> expected = new[]
            {
                (PkmnSpecies.OfId("2"), 1),
                (PkmnSpecies.OfId("3"), 3),
            }.ToImmutableSortedDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public async Task can_check_if_user_has_badge()
        {
            IBadgeRepo badgeRepo = CreateBadgeRepo();
            // given
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("1"), Badge.BadgeSource.Pinball);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball);

            // when
            bool hasUserSpecies1 = await badgeRepo.HasUserBadge("user", PkmnSpecies.OfId("1"));
            bool hasUserSpecies2 = await badgeRepo.HasUserBadge("user", PkmnSpecies.OfId("2"));
            bool hasUserSpecies3 = await badgeRepo.HasUserBadge("user", PkmnSpecies.OfId("3"));
            bool hasUserSpecies4 = await badgeRepo.HasUserBadge("user", PkmnSpecies.OfId("4"));

            // then
            Assert.IsFalse(hasUserSpecies1);
            Assert.IsTrue(hasUserSpecies2);
            Assert.IsTrue(hasUserSpecies3);
            Assert.IsFalse(hasUserSpecies4);
        }

        [TestFixture]
        private class TransferBadge : MongoTestBase
        {
            [Test]
            public async Task returns_updated_badge_object()
            {
                IBadgeRepo badgeRepo = new BadgeRepo(
                    CreateTemporaryDatabase(), Mock.Of<IMongoBadgeLogRepo>(), Mock.Of<IClock>());
                Badge badge = await badgeRepo.AddBadge(
                    "user", PkmnSpecies.OfId("1"), Badge.BadgeSource.ManualCreation);

                IImmutableList<Badge> updatedBadges = await badgeRepo.TransferBadges(
                    ImmutableList.Create(badge), "recipient", "reason", new Dictionary<string, object?>());

                Assert.AreEqual(1, updatedBadges.Count);
                Assert.AreEqual(badge.Id, updatedBadges[0].Id);
                Assert.AreEqual(badge.Species, updatedBadges[0].Species);
                Assert.AreEqual(badge.Source, updatedBadges[0].Source);
                Assert.AreEqual(badge.CreatedAt, updatedBadges[0].CreatedAt);
                Assert.AreEqual("recipient", updatedBadges[0].UserId);
            }

            [Test]
            public async Task unmarks_as_selling()
            {
                BadgeRepo badgeRepo = new(CreateTemporaryDatabase(), Mock.Of<IMongoBadgeLogRepo>(), Mock.Of<IClock>());
                Badge badge = await badgeRepo.AddBadge(
                    "user", PkmnSpecies.OfId("1"), Badge.BadgeSource.ManualCreation);
                await badgeRepo.Collection.UpdateOneAsync(
                    Builders<Badge>.Filter.Where(b => b.Id == badge.Id),
                    Builders<Badge>.Update
                        .Set(b => b.SellingSince, Instant.FromUnixTimeSeconds(0))
                        .Set(b => b.SellPrice, 123));

                IImmutableList<Badge> updatedBadges = await badgeRepo.TransferBadges(
                    ImmutableList.Create(badge), "recipient", "reason", new Dictionary<string, object?>());

                Assert.AreEqual(1, updatedBadges.Count);
                Assert.IsNull(updatedBadges[0].SellingSince);
                Assert.IsNull(updatedBadges[0].SellPrice);
                Badge updatedBadge = await badgeRepo.Collection.Find(b => b.Id == badge.Id).FirstAsync();
                Assert.AreEqual(updatedBadge, updatedBadges[0]);
                Assert.IsNull(updatedBadge.SellingSince);
                Assert.IsNull(updatedBadge.SellPrice);
            }

            [Test]
            public async Task logs_to_badgelog()
            {
                Mock<IClock> clockMock = new();
                Mock<IMongoBadgeLogRepo> mongoBadgeLogRepoMock = new();
                BadgeRepo badgeRepo = new(CreateTemporaryDatabase(), mongoBadgeLogRepoMock.Object, clockMock.Object);
                Badge badge = await badgeRepo.AddBadge(
                    "user", PkmnSpecies.OfId("1"), Badge.BadgeSource.ManualCreation);

                Instant timestamp = Instant.FromUnixTimeSeconds(123);
                clockMock.Setup(c => c.GetCurrentInstant()).Returns(timestamp);

                IDictionary<string, object?> data = new Dictionary<string, object?> { ["some"] = "data" };
                await badgeRepo.TransferBadges(ImmutableList.Create(badge), "recipient", "reason", data);

                mongoBadgeLogRepoMock.Verify(l => l.LogWithSession(
                        badge.Id, "reason", "recipient", timestamp, data, It.IsAny<IClientSessionHandle>()),
                    Times.Once);
            }

            [Test]
            public async Task triggers_species_lost_event()
            {
                Mock<IMongoBadgeLogRepo> mongoBadgeLogRepoMock = new();
                BadgeRepo badgeRepo = new(CreateTemporaryDatabase(), mongoBadgeLogRepoMock.Object, Mock.Of<IClock>());
                PkmnSpecies species = PkmnSpecies.OfId("1");
                Badge badge1 = await badgeRepo.AddBadge("user", species, Badge.BadgeSource.ManualCreation);
                Badge badge2 = await badgeRepo.AddBadge("user", species, Badge.BadgeSource.ManualCreation);
                int userLostBadgeInvocations = 0;
                badgeRepo.UserLostBadgeSpecies += (_, args) =>
                {
                    Assert.AreEqual("user", args.UserId);
                    Assert.AreEqual(species, args.Species);
                    userLostBadgeInvocations++;
                };

                await badgeRepo.TransferBadges(
                    ImmutableList.Create(badge1), "recipient", "reason", new Dictionary<string, object?>());
                Assert.AreEqual(0, userLostBadgeInvocations, "one badge of species left");
                await badgeRepo.TransferBadges(
                    ImmutableList.Create(badge2), "recipient", "reason", new Dictionary<string, object?>());
                Assert.AreEqual(1, userLostBadgeInvocations, "last badge of species lost");
            }

            [Test]
            public async Task aborts_all_transfers_if_one_fails()
            {
                Mock<IMongoBadgeLogRepo> mongoBadgeLogRepoMock = new();
                BadgeRepo badgeRepo = new(CreateTemporaryDatabase(), mongoBadgeLogRepoMock.Object, Mock.Of<IClock>());
                PkmnSpecies species = PkmnSpecies.OfId("1");
                Badge badge1 = await badgeRepo.AddBadge("user", species, Badge.BadgeSource.ManualCreation);
                Badge badge2 = await badgeRepo.AddBadge("user", species, Badge.BadgeSource.ManualCreation);
                // make in-memory badge reference stale to cause the transfer to fail on the second badge
                await badgeRepo.Collection.UpdateOneAsync(
                    Builders<Badge>.Filter.Where(b => b.Id == badge2.Id),
                    Builders<Badge>.Update.Set(b => b.UserId, "someOtherUser"));

                OwnedBadgeNotFoundException ex = Assert.ThrowsAsync<OwnedBadgeNotFoundException>(() =>
                    badgeRepo.TransferBadges(ImmutableList.Create(badge1, badge2),
                        "recipient", "reason", new Dictionary<string, object?>()));
                Assert.AreEqual(badge2, ex.Badge);
                // first badge must not have changed ownership
                Badge firstBadge = await badgeRepo.Collection.Find(b => b.Id == badge1.Id).FirstAsync();
                Assert.AreEqual("user", firstBadge.UserId);
                Assert.AreEqual(badge1, firstBadge);
            }
        }

        [TestFixture]
        private class BadgeStats : MongoTestBase
        {
            [Test]
            public async Task ignore_lapsed_for_rarity_counts()
            {
                /*
                 *                UPDATE   NOW
                 * -20     -10      0       10
                 *  |-------|-------|-------|
                 *      |-----transition----|
                 */
                Mock<IClock> clockMock = new();
                Duration transition = Duration.FromSeconds(25);
                Instant tBeforeUpdateLapsed = Instant.FromUnixTimeSeconds(-20);
                Instant tBeforeUpdateConsidered = Instant.FromUnixTimeSeconds(-10);
                Instant tUpdate = Instant.FromUnixTimeSeconds(0);
                Instant tNow = Instant.FromUnixTimeSeconds(10);
                clockMock.Setup(c => c.GetCurrentInstant()).Returns(tNow);

                BadgeRepo badgeRepo = new(CreateTemporaryDatabase(), Mock.Of<IMongoBadgeLogRepo>(),
                    clockMock.Object, lastRarityUpdate: tUpdate, rarityCalculationTransition: transition);

                PkmnSpecies species = PkmnSpecies.OfId("1-testlapsed");
                await badgeRepo.AddBadge("user", species, Badge.BadgeSource.Pinball, tBeforeUpdateLapsed);
                await badgeRepo.AddBadge("user", species, Badge.BadgeSource.Pinball, tBeforeUpdateConsidered);
                await badgeRepo.AddBadge("user", species, Badge.BadgeSource.Pinball, tUpdate);
                await badgeRepo.AddBadge("user", species, Badge.BadgeSource.Pinball, tNow);

                await badgeRepo.RenewBadgeStats(onlyTheseSpecies: ImmutableHashSet.Create(species));
                ImmutableSortedDictionary<PkmnSpecies, BadgeStat> stats = await badgeRepo.GetBadgeStats();
                BadgeStat stat = stats[species];
                Assert.AreEqual((4, 4, 3, 3),
                    (stat.Count, stat.CountGenerated, stat.RarityCount, stat.RarityCountGenerated));
            }

            [Test]
            public async Task ignore_destroyed_for_regular_count()
            {
                BadgeRepo badgeRepo = new(CreateTemporaryDatabase(), Mock.Of<IMongoBadgeLogRepo>(),
                    Mock.Of<IClock>());

                PkmnSpecies species = PkmnSpecies.OfId("1-testdestroyed");
                await badgeRepo.AddBadge("user", species, Badge.BadgeSource.Pinball);
                // second badge has no owner
                await badgeRepo.AddBadge(null, species, Badge.BadgeSource.Pinball);

                await badgeRepo.RenewBadgeStats(onlyTheseSpecies: ImmutableHashSet.Create(species));
                ImmutableSortedDictionary<PkmnSpecies, BadgeStat> stats = await badgeRepo.GetBadgeStats();
                BadgeStat stat = stats[species];
                Assert.AreEqual((1, 2, 1, 2),
                    (stat.Count, stat.CountGenerated, stat.RarityCount, stat.RarityCountGenerated));
            }

            [Test]
            public async Task ignore_unnatural_sources_for_generated_count()
            {
                BadgeRepo badgeRepo = new BadgeRepo(CreateTemporaryDatabase(), Mock.Of<IMongoBadgeLogRepo>(),
                    Mock.Of<IClock>());

                PkmnSpecies species = PkmnSpecies.OfId("1-testunnatural");
                await badgeRepo.AddBadge("user", species, Badge.BadgeSource.Pinball);
                // second badge has source 'transmutation', which is not a natural source
                await badgeRepo.AddBadge("user", species, Badge.BadgeSource.Transmutation);

                await badgeRepo.RenewBadgeStats(onlyTheseSpecies: ImmutableHashSet.Create(species));
                ImmutableSortedDictionary<PkmnSpecies, BadgeStat> stats = await badgeRepo.GetBadgeStats();
                BadgeStat stat = stats[species];
                Assert.AreEqual((2, 1, 2, 1),
                    (stat.Count, stat.CountGenerated, stat.RarityCount, stat.RarityCountGenerated));
            }

            [Test]
            public async Task incorporate_source_and_existence_in_rarity()
            {
                BadgeRepo badgeRepo = new(CreateTemporaryDatabase(), Mock.Of<IMongoBadgeLogRepo>(),
                    Mock.Of<IClock>());

                PkmnSpecies species1 = PkmnSpecies.OfId("1-testrarity");
                PkmnSpecies species2 = PkmnSpecies.OfId("2-testrarity");
                PkmnSpecies species3 = PkmnSpecies.OfId("3-testrarity");

                await badgeRepo.AddBadge("user", species1, Badge.BadgeSource.Pinball);
                await badgeRepo.AddBadge("user", species1, Badge.BadgeSource.Transmutation);

                await badgeRepo.AddBadge("user", species2, Badge.BadgeSource.Pinball);
                await badgeRepo.AddBadge("user", species2, Badge.BadgeSource.Transmutation);
                await badgeRepo.AddBadge("user", species2, Badge.BadgeSource.Transmutation);
                await badgeRepo.AddBadge(null, species2, Badge.BadgeSource.Transmutation);

                await badgeRepo.AddBadge(null, species3, Badge.BadgeSource.Pinball);
                await badgeRepo.AddBadge("user", species3, Badge.BadgeSource.Pinball);
                await badgeRepo.AddBadge("user", species3, Badge.BadgeSource.Transmutation);

                await badgeRepo.RenewBadgeStats();
                ImmutableSortedDictionary<PkmnSpecies, BadgeStat> stats = await badgeRepo.GetBadgeStats();
                Assert.AreEqual(3, stats.Count);

                {
                    // species 1
                    BadgeStat stat = stats[species1];
                    Assert.AreEqual((2, 1), (stat.RarityCount, stat.RarityCountGenerated));
                    // 1 out of all 4 badges ever generated (4x pinball), weighted at 80%,
                    // plus 2 out of all 7 currently existing badges (3x pinball, 4x transmutation), weighted at 20%.
                    Assert.AreEqual(0.8d * (1 / 4d) + 0.2d * (2 / 7d), stat.Rarity);
                }
                {
                    // species 2
                    BadgeStat stat = stats[species2];
                    Assert.AreEqual((3, 1), (stat.RarityCount, stat.RarityCountGenerated));
                    // almost same as first species: has 3/7 instead of 2/7 existing badges
                    Assert.AreEqual(0.8d * (1 / 4d) + 0.2d * (3 / 7d), stat.Rarity);
                }
                {
                    // species 3
                    BadgeStat stat = stats[species3];
                    Assert.AreEqual((2, 2), (stat.RarityCount, stat.RarityCountGenerated));
                    // almost same as first species: has 2/4 instead of 1/4 generated badges
                    Assert.AreEqual(0.8d * (2 / 4d) + 0.2d * (2 / 7d), stat.Rarity);
                }
            }
        }
    }
}
