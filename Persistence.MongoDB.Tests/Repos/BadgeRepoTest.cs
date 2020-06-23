using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Persistence.Models;
using Persistence.MongoDB.Repos;
using Persistence.MongoDB.Serializers;

namespace Persistence.MongoDB.Tests.Repos
{
    [Category("IntegrationTest")]
    public class BadgeRepoTest : MongoTestBase
    {
        private IMongoDatabase _database = null!;
        private BadgeRepo _badgeRepo = null!;

        [OneTimeSetUp]
        public void SetUpSerializers() => CustomSerializers.RegisterAll();

        [SetUp]
        public void SetUp()
        {
            _database = CreateTemporaryDatabase();
            _badgeRepo = new BadgeRepo(_database);
        }

        [Test]
        public async Task TestInsert()
        {
            // when
            Badge badge = await _badgeRepo.AddBadge(null, PkmnSpecies.OfId("16"), Badge.BadgeSource.ManualCreation);

            // then
            Assert.AreNotEqual(string.Empty, badge.Id);
            Badge badgeFromDatabase = await _badgeRepo.Collection.Find(b => b.Id == badge.Id).FirstOrDefaultAsync();
            Assert.NotNull(badgeFromDatabase);
            Assert.AreNotSame(badgeFromDatabase, badge);
            Assert.AreEqual(badgeFromDatabase, badge);
        }

        /// <summary>
        /// Tests that the data gets represented in the database the desired way.
        /// This ensures backwards compatibility to any existing data.
        /// </summary>
        [Test]
        public async Task TestDatabaseSerialization()
        {
            // when
            PkmnSpecies randomSpecies = PkmnSpecies.OfId("9001");
            Badge badge = await _badgeRepo.AddBadge(null, randomSpecies, Badge.BadgeSource.RunCaught);

            // then
            IMongoCollection<BsonDocument> badgesCollectionBson = _database.GetCollection<BsonDocument>("badges");
            BsonDocument badgeBson = await badgesCollectionBson.Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();
            Assert.AreEqual(BsonObjectId.Create(ObjectId.Parse(badge.Id)), badgeBson["_id"]);
            Assert.AreEqual(BsonNull.Value, badgeBson["user"]);
            Assert.AreEqual(BsonString.Create(randomSpecies.Id), badgeBson["species"]);
            Assert.AreEqual(BsonString.Create("run_caught"), badgeBson["source"]);
        }

        [Test]
        public async Task TestFindByUser()
        {
            // given
            Badge badgeUserA1 = await _badgeRepo.AddBadge("userA", PkmnSpecies.OfId("1"), Badge.BadgeSource.Pinball);
            Badge badgeUserA2 = await _badgeRepo.AddBadge("userA", PkmnSpecies.OfId("2"), Badge.BadgeSource.Pinball);
            Badge badgeUserB = await _badgeRepo.AddBadge("userB", PkmnSpecies.OfId("3"), Badge.BadgeSource.Pinball);
            Badge badgeNobody = await _badgeRepo.AddBadge(null, PkmnSpecies.OfId("4"), Badge.BadgeSource.Pinball);

            // when
            List<Badge> resultUserA = await _badgeRepo.FindByUser("userA");
            List<Badge> resultUserB = await _badgeRepo.FindByUser("userB");
            List<Badge> resultNobody = await _badgeRepo.FindByUser(null);

            // then
            Assert.AreEqual(new List<Badge> {badgeUserA1, badgeUserA2}, resultUserA);
            Assert.AreEqual(new List<Badge> {badgeUserB}, resultUserB);
            Assert.AreEqual(new List<Badge> {badgeNobody}, resultNobody);
        }
    }
}
