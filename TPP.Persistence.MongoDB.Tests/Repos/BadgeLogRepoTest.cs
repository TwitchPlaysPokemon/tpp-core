using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NodaTime;
using NUnit.Framework;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos
{
    public class BadgeLogRepoTest : MongoTestBase
    {
        [Test]
        public async Task persists_successfully()
        {
            BadgeLogRepo repo = new(CreateTemporaryDatabase());
            string badgeId = ObjectId.GenerateNewId().ToString();
            const string badgeLogType = "type";
            const string userId = "user";
            Instant timestamp = Instant.FromUnixTimeSeconds(123);

            // persist to db
            IDictionary<string, object?> data = new Dictionary<string, object?> { ["some"] = "data" };
            BadgeLog written = await repo.Log(badgeId, badgeLogType, userId, timestamp, data);
            Assert.AreEqual(badgeId, written.BadgeId);
            Assert.AreEqual(badgeLogType, written.BadgeLogType);
            Assert.AreEqual(userId, written.UserId);
            Assert.AreEqual(timestamp, written.Timestamp);
            Assert.AreEqual(data, written.AdditionalData);
            Assert.NotNull(written.Id);

            // read from db
            List<BadgeLog> allItems = await repo.Collection.Find(FilterDefinition<BadgeLog>.Empty).ToListAsync();
            Assert.AreEqual(1, allItems.Count);
            BadgeLog read = allItems[0];
            Assert.AreEqual(written, read);
            Assert.AreEqual(badgeId, read.BadgeId);
            Assert.AreEqual(badgeLogType, read.BadgeLogType);
            Assert.AreEqual(userId, read.UserId);
            Assert.AreEqual(timestamp, read.Timestamp);
            Assert.AreEqual(data, read.AdditionalData);
        }
    }
}
