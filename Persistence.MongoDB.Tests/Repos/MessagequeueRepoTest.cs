using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NUnit.Framework;
using Persistence.Models;
using Persistence.MongoDB.Repos;

namespace Persistence.MongoDB.Tests.Repos
{
    public class MessagequeueRepoTest : MongoTestBase
    {
        [Test]
        public async Task persists_successfully()
        {
            MessagequeueRepo repo = new(CreateTemporaryDatabase());
            const string ircLine = "some text";

            // persist to db
            MessagequeueItem written = await repo.EnqueueMessage(ircLine);
            Assert.AreEqual(ircLine, written.IrcLine);
            Assert.NotNull(written.Id);

            // read from db
            List<MessagequeueItem> allItems = await repo.Collection
                .Find(FilterDefinition<MessagequeueItem>.Empty).ToListAsync();
            Assert.AreEqual(1, allItems.Count);
            MessagequeueItem read = allItems[0];
            Assert.AreEqual(written, read);
            Assert.AreEqual(ircLine, read.IrcLine);
        }
    }
}
