using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NodaTime;
using NUnit.Framework;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos
{
    public class MessagelogRepoTest : MongoTestBase
    {
        [Test]
        public async Task persists_successfully()
        {
            MessagelogRepo repo = new(CreateTemporaryDatabase());
            const string userId = "123";
            const string ircLine = "irc line text";
            const string message = "message text";
            Instant timestamp = Instant.FromUnixTimeSeconds(123);

            // persist to db
            Messagelog written = await repo.LogChat(userId, ircLine, message, timestamp);
            Assert.AreEqual(userId, written.UserId);
            Assert.AreEqual(ircLine, written.IrcLine);
            Assert.AreEqual(message, written.Message);
            Assert.AreEqual(timestamp, written.Timestamp);
            Assert.NotNull(written.Id);

            // read from db
            List<Messagelog> allItems = await repo.Collection.Find(FilterDefinition<Messagelog>.Empty).ToListAsync();
            Assert.AreEqual(1, allItems.Count);
            Messagelog read = allItems[0];
            Assert.AreEqual(written, read);
            Assert.AreEqual(userId, read.UserId);
            Assert.AreEqual(ircLine, read.IrcLine);
            Assert.AreEqual(message, read.Message);
            Assert.AreEqual(timestamp, read.Timestamp);
        }
    }
}
