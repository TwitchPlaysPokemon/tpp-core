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
            Badge badge = await badgeRepo.AddBadge(null, PkmnSpecies.OfId("16"), Badge.BadgeSource.ManualCreation, Badge.BadgeForm.Normal);

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

            Badge badge = await badgeRepo.AddBadge(null, PkmnSpecies.OfId("16"), Badge.BadgeSource.ManualCreation, Badge.BadgeForm.Normal);
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
            Badge badge = await badgeRepo.AddBadge(null, randomSpecies, Badge.BadgeSource.RunCaught, Badge.BadgeForm.Normal);

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
            Badge badgeUserA1 = await badgeRepo.AddBadge("userA", PkmnSpecies.OfId("1"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            Badge badgeUserA2 = await badgeRepo.AddBadge("userA", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            Badge badgeUserB = await badgeRepo.AddBadge("userB", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            Badge badgeNobody = await badgeRepo.AddBadge(null, PkmnSpecies.OfId("4"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);

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
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("1"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);

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
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("1"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);

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
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("user", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("1"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);
            await badgeRepo.AddBadge("userOther", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball, Badge.BadgeForm.Normal);

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
                    "user", PkmnSpecies.OfId("1"), Badge.BadgeSource.ManualCreation, Badge.BadgeForm.Normal);

                IImmutableList<Badge> updatedBadges = await badgeRepo.TransferBadges(
                    ImmutableList.Create(badge), "recipient", "reason", new Dictionary<string, object?>());

                Assert.AreEqual(1, updatedBadges.Count);
                Assert.AreEqual(badge.Id, updatedBadges[0].Id);
                Assert.AreEqual(badge.Species, updatedBadges[0].Species);
                Assert.AreEqual(badge.Source, updatedBadges[0].Source);
                Assert.AreEqual(badge.CreatedAt, updatedBadges[0].CreatedAt);
                Assert.AreEqual("recipient", updatedBadges[0].UserId);
                Assert.AreEqual(badge.Form, updatedBadges[0].Form);
            }

            [Test]
            public async Task unmarks_as_selling()
            {
                BadgeRepo badgeRepo = new(CreateTemporaryDatabase(), Mock.Of<IMongoBadgeLogRepo>(), Mock.Of<IClock>());
                Badge badge = await badgeRepo.AddBadge(
                    "user", PkmnSpecies.OfId("1"), Badge.BadgeSource.ManualCreation, Badge.BadgeForm.Normal);
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
                    "user", PkmnSpecies.OfId("1"), Badge.BadgeSource.ManualCreation, Badge.BadgeForm.Normal);

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
                Badge badge1 = await badgeRepo.AddBadge("user", species, Badge.BadgeSource.ManualCreation, Badge.BadgeForm.Normal);
                Badge badge2 = await badgeRepo.AddBadge("user", species, Badge.BadgeSource.ManualCreation, Badge.BadgeForm.Normal);
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
                Badge badge1 = await badgeRepo.AddBadge("user", species, Badge.BadgeSource.ManualCreation, Badge.BadgeForm.Normal);
                Badge badge2 = await badgeRepo.AddBadge("user", species, Badge.BadgeSource.ManualCreation, Badge.BadgeForm.Normal);
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
    }
}
