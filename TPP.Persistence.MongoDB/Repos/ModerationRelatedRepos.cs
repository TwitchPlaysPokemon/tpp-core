using System.Diagnostics;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos;

public class ModbotLogRepo(IMongoDatabase database) : IModbotLogRepo, IAsyncInitRepo
{
    private const string CollectionName = "modlog";

    public readonly IMongoCollection<ModbotLog> Collection = database.GetCollection<ModbotLog>(CollectionName);

    static ModbotLogRepo()
    {
        BsonClassMap.RegisterClassMap<ModbotLog>(cm =>
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

    public async Task InitializeAsync()
    {
        await Collection.Indexes.CreateManyAsync([
            new CreateIndexModel<ModbotLog>(Builders<ModbotLog>.IndexKeys.Ascending(u => u.UserId)),
            new CreateIndexModel<ModbotLog>(Builders<ModbotLog>.IndexKeys.Descending(u => u.Timestamp))
        ]);
    }

    public async Task<ModbotLog> LogAction(User user, string reason, string rule, Instant timestamp)
    {
        var modLog = new ModbotLog(string.Empty, user.Id, reason, rule, timestamp);
        await Collection.InsertOneAsync(modLog);
        Debug.Assert(modLog.Id.Length > 0, "The MongoDB driver injected a generated ID");
        return modLog;
    }

    public Task<long> CountRecentBans(User user, Instant cutoff)
    {
        return Collection.CountDocumentsAsync(log => log.UserId == user.Id && log.Timestamp >= cutoff);
    }
}

public class BanLogRepo(IMongoDatabase database) : IBanLogRepo, IAsyncInitRepo
{
    private const string CollectionName = "banlog";

    public readonly IMongoCollection<BanLog> Collection = database.GetCollection<BanLog>(CollectionName);

    static BanLogRepo()
    {
        BsonClassMap.RegisterClassMap<BanLog>(cm =>
        {
            cm.MapIdProperty(b => b.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(b => b.Type).SetElementName("type");
            cm.MapProperty(b => b.UserId).SetElementName("user");
            cm.MapProperty(b => b.Reason).SetElementName("reason");
            cm.MapProperty(b => b.IssuerUserId).SetElementName("issuer");
            cm.MapProperty(b => b.Timestamp).SetElementName("timestamp");
        });
    }

    public async Task InitializeAsync()
    {
        await Collection.Indexes.CreateManyAsync([
            new CreateIndexModel<BanLog>(Builders<BanLog>.IndexKeys.Ascending(u => u.UserId)),
            new CreateIndexModel<BanLog>(Builders<BanLog>.IndexKeys.Descending(u => u.Timestamp))
        ]);
    }

    public async Task<BanLog> LogBan(string userId, string type, string reason, string? issuerUserId, Instant timestamp)
    {
        var banLog = new BanLog(string.Empty, type, userId, reason, issuerUserId, timestamp);
        await Collection.InsertOneAsync(banLog);
        Debug.Assert(banLog.Id.Length > 0, "The MongoDB driver injected a generated ID");
        return banLog;
    }

    public async Task<BanLog?> FindMostRecent(string userId) => await Collection
        .Find(log => log.UserId == userId)
        .SortByDescending(log => log.Timestamp)
        .FirstOrDefaultAsync();
}

public class TimeoutLogRepo(IMongoDatabase database) : ITimeoutLogRepo, IAsyncInitRepo
{
    private const string CollectionName = "timeoutlog";

    public readonly IMongoCollection<TimeoutLog> Collection = database.GetCollection<TimeoutLog>(CollectionName);

    static TimeoutLogRepo()
    {
        BsonClassMap.RegisterClassMap<TimeoutLog>(cm =>
        {
            cm.MapIdProperty(b => b.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(b => b.Type).SetElementName("type");
            cm.MapProperty(b => b.UserId).SetElementName("user");
            cm.MapProperty(b => b.Reason).SetElementName("reason");
            cm.MapProperty(b => b.IssuerUserId).SetElementName("issuer");
            cm.MapProperty(b => b.Timestamp).SetElementName("timestamp");
            cm.MapProperty(b => b.Duration).SetElementName("duration")
                .SetSerializer(NullableDurationAsSecondsSerializer.Instance);
        });
    }

    public async Task InitializeAsync()
    {
        await Collection.Indexes.CreateManyAsync([
            new CreateIndexModel<TimeoutLog>(Builders<TimeoutLog>.IndexKeys.Ascending(u => u.UserId)),
            new CreateIndexModel<TimeoutLog>(Builders<TimeoutLog>.IndexKeys.Descending(u => u.Timestamp))
        ]);
    }

    public async Task<TimeoutLog> LogTimeout(
        string userId, string type, string reason, string? issuerUserId, Instant timestamp, Duration? duration)
    {
        var banLog = new TimeoutLog(string.Empty, type, userId, reason, issuerUserId, timestamp, duration);
        await Collection.InsertOneAsync(banLog);
        Debug.Assert(banLog.Id.Length > 0, "The MongoDB driver injected a generated ID");
        return banLog;
    }

    public async Task<TimeoutLog?> FindMostRecent(string userId) => await Collection
        .Find(log => log.UserId == userId)
        .SortByDescending(log => log.Timestamp)
        .FirstOrDefaultAsync();
}
