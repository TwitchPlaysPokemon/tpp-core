using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Persistence.MongoDB.Repos;

namespace Persistence.MongoDB.Tests.Repos
{
    public class BadgeRepoTest : MongoTestBase
    {
        private IMongoDatabase _database  = null!;
        private BadgeRepo      _badgeRepo = null!;

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
            var badge = await _badgeRepo.AddBadge(null, "16", Badge.BadgeSource.ManualCreation);

            // then
            Assert.AreNotEqual(string.Empty, badge.Id);
            var badgeFromDatabase = await _badgeRepo.Collection.Find(b => b.Id == badge.Id).FirstAsync();
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
            string randomSpecies = Guid.NewGuid().ToString();
            var badge = await _badgeRepo.AddBadge(null, randomSpecies, Badge.BadgeSource.RunCaught);

            // then
            var badgesCollectionBson = _database.GetCollection<BsonDocument>("badges");
            var badgeBson = await badgesCollectionBson.Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();
            Assert.AreEqual(BsonObjectId.Create(ObjectId.Parse(badge.Id)), badgeBson["_id"]);
            Assert.AreEqual(BsonNull.Value, badgeBson["user"]);
            Assert.AreEqual(BsonString.Create(randomSpecies), badgeBson["species"]);
            Assert.AreEqual(BsonString.Create("run_caught"), badgeBson["source"]);
        }

        [Test]
        public async Task TestFindByUser()
        {
            // given
            var badgeUserA1 = await _badgeRepo.AddBadge("userA", "1", Badge.BadgeSource.Pinball);
            var badgeUserA2 = await _badgeRepo.AddBadge("userA", "2", Badge.BadgeSource.Pinball);
            var badgeUserB = await _badgeRepo.AddBadge("userB", "3", Badge.BadgeSource.Pinball);
            var badgeNobody = await _badgeRepo.AddBadge(null, "4", Badge.BadgeSource.Pinball);

            // when
            var resultUserA = await _badgeRepo.FindByUser("userA");
            var resultUserB = await _badgeRepo.FindByUser("userB");
            var resultNobody = await _badgeRepo.FindByUser(null);

            // then
            Assert.AreEqual(new List<Badge> {badgeUserA1, badgeUserA2}, resultUserA);
            Assert.AreEqual(new List<Badge> {badgeUserB}, resultUserB);
            Assert.AreEqual(new List<Badge> {badgeNobody}, resultNobody);
        }
    }
}
