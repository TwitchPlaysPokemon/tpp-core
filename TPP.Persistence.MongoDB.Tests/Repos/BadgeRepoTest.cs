using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos
{
    [Category("IntegrationTest")]
    [Parallelizable(ParallelScope.All)]
    public class BadgeRepoTest : MongoTestBase
    {
        [Test]
        public async Task insert_then_read_are_equal()
        {
            var badgeRepo = new BadgeRepo(CreateTemporaryDatabase());
            // when
            Badge badge = await badgeRepo.AddBadge(null, PkmnSpecies.OfId("16"), Badge.BadgeSource.ManualCreation);

            // then
            Assert.AreNotEqual(string.Empty, badge.Id);
            Badge badgeFromDatabase = await badgeRepo.Collection.Find(b => b.Id == badge.Id).FirstOrDefaultAsync();
            Assert.NotNull(badgeFromDatabase);
            Assert.AreNotSame(badgeFromDatabase, badge);
            Assert.AreEqual(badgeFromDatabase, badge);
        }

        /// <summary>
        /// Tests that the data gets represented in the database the desired way.
        /// This ensures backwards compatibility to any existing data.
        /// </summary>
        [Test]
        public async Task has_expected_bson_datatypes()
        {
            var badgeRepo = new BadgeRepo(CreateTemporaryDatabase());
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
            var badgeRepo = new BadgeRepo(CreateTemporaryDatabase());
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
            var badgeRepo = new BadgeRepo(CreateTemporaryDatabase());
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
            var badgeRepo = new BadgeRepo(CreateTemporaryDatabase());
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
            var badgeRepo = new BadgeRepo(CreateTemporaryDatabase());
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
    }
}
