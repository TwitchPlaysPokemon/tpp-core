namespace TPP.Persistence.MongoDB.Tests.Repos;

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
        Assert.That(written.BadgeId, Is.EqualTo(badgeId));
        Assert.That(written.BadgeLogType, Is.EqualTo(badgeLogType));
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.That(written.AdditionalData, Is.EqualTo(data));
        Assert.NotNull(written.Id);

        // read from db
        List<BadgeLog> allItems = await repo.Collection.Find(FilterDefinition<BadgeLog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        BadgeLog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.BadgeId, Is.EqualTo(badgeId));
        Assert.That(read.BadgeLogType, Is.EqualTo(badgeLogType));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
        Assert.That(read.AdditionalData, Is.EqualTo(data));
    }
}
