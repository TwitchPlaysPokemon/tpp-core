using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NodaTime;
using NUnit.Framework;
using Model;
using Persistence;
using PersistenceMongoDB.Repos;

namespace PersistenceMongoDB.Tests.Repos;

public class TransmutationLogRepoTest : MongoTestBase
{
    [Test]
    public async Task persists_successfully()
    {
        TransmutationLogRepo repo = new(CreateTemporaryDatabase());
        Instant timestamp = Instant.FromUnixTimeSeconds(123);
        const string userId = "123";
        const int cost = 1;
        IReadOnlyList<string> inputBadges = new List<string>
        {
            ObjectId.GenerateNewId().ToString(),
            ObjectId.GenerateNewId().ToString(),
            ObjectId.GenerateNewId().ToString(),
        };
        string outputBadge = ObjectId.GenerateNewId().ToString();

        // persist to db
        TransmutationLog written = await repo.Log(userId, timestamp, cost, inputBadges, outputBadge);
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.Cost, Is.EqualTo(cost));
        Assert.That(written.InputBadges, Is.EqualTo(inputBadges));
        Assert.That(written.OutputBadge, Is.EqualTo(outputBadge));
        Assert.That(written.Id, Is.Not.Null);

        // read from db
        List<TransmutationLog> allItems =
            await repo.Collection.Find(FilterDefinition<TransmutationLog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        TransmutationLog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));

        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.Cost, Is.EqualTo(cost));
        Assert.That(read.InputBadges, Is.EqualTo(inputBadges));
        Assert.That(read.OutputBadge, Is.EqualTo(outputBadge));
    }
}
