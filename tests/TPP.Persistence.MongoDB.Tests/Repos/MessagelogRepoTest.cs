using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NodaTime;
using NUnit.Framework;
using TPP.Model;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos;

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
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.IrcLine, Is.EqualTo(ircLine));
        Assert.That(written.Message, Is.EqualTo(message));
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.NotNull(written.Id);

        // read from db
        List<Messagelog> allItems = await repo.Collection.Find(FilterDefinition<Messagelog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        Messagelog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.IrcLine, Is.EqualTo(ircLine));
        Assert.That(read.Message, Is.EqualTo(message));
        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
    }
}
