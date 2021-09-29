namespace TPP.Persistence.MongoDB.Tests.Repos;

public class MessagequeueRepoTest : MongoTestBase
{
    [Test]
    public async Task persists_successfully()
    {
        MessagequeueRepo repo = new(CreateTemporaryDatabase());
        const string ircLine = "some text";

        // persist to db
        MessagequeueItem written = await repo.EnqueueMessage(ircLine);
        Assert.That(written.IrcLine, Is.EqualTo(ircLine));
        Assert.NotNull(written.Id);

        // read from db
        List<MessagequeueItem> allItems = await repo.Collection
            .Find(FilterDefinition<MessagequeueItem>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        MessagequeueItem read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.IrcLine, Is.EqualTo(ircLine));
    }
}
