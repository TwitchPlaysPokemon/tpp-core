using System.Diagnostics;

namespace TPP.Persistence.MongoDB.Repos;

public class ModLogRepo : IModLogRepo
{
    private const string CollectionName = "modlog";

    public readonly IMongoCollection<ModLog> Collection;

    static ModLogRepo()
    {
        BsonClassMap.RegisterClassMap<ModLog>(cm =>
        {
            cm.MapIdProperty(b => b.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(b => b.UserId).SetElementName("user");
            cm.MapProperty(b => b.Reason).SetElementName("reason");
            cm.MapProperty(b => b.Rule).SetElementName("rule");
            cm.MapProperty(b => b.Timestamp).SetElementName("ts");
        });
    }

    public ModLogRepo(IMongoDatabase database)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<ModLog>(CollectionName);
        InitIndexes();
    }

    private void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<ModLog>(Builders<ModLog>.IndexKeys.Ascending(u => u.UserId)),
            new CreateIndexModel<ModLog>(Builders<ModLog>.IndexKeys.Descending(u => u.Timestamp)),
        });
    }

    public async Task<ModLog> LogModAction(User user, string reason, string rule, Instant timestamp)
    {
        var modLog = new ModLog(string.Empty, user.Id, reason, rule, timestamp);
        await Collection.InsertOneAsync(modLog);
        Debug.Assert(modLog.Id.Length > 0, "The MongoDB driver injected a generated ID");
        return modLog;
    }

    public Task<long> CountRecentBans(User user, Instant cutoff)
    {
        return Collection.CountDocumentsAsync(log => log.UserId == user.Id && log.Timestamp >= cutoff);
    }
}
