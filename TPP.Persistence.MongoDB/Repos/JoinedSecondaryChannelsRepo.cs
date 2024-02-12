using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NodaTime;

namespace TPP.Persistence.MongoDB.Repos;

public class JoinedSecondaryChannelsRepo : IJoinedSecondaryChannelsRepo
{
    private const string CollectionName = "joined_secondary_channels";
    private const string LogCollectionName = "joined_secondary_channels_log";

    public readonly IMongoCollection<BsonDocument> Collection;
    public readonly IMongoCollection<BsonDocument> LogCollection;

    public JoinedSecondaryChannelsRepo(IMongoDatabase database)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<BsonDocument>(CollectionName);
        LogCollection = database.GetCollection<BsonDocument>(LogCollectionName);
    }

    public Task<bool> IsJoined(string channelName) =>
        Collection.Find(doc => doc["_id"] == channelName).AnyAsync();

    public async Task<IImmutableSet<string>> GetJoinedChannels() =>
        (await Collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())
        .Select(doc => doc["_id"].AsString).ToImmutableHashSet();

    public async Task Add(string channelName)
    {
        await Collection.InsertOneAsync(new BsonDocument { ["_id"] = channelName });
        await LogJoin(channelName);
    }

    public async Task Remove(string channelName)
    {
        await Collection.DeleteOneAsync(doc => doc["_id"] == channelName);
        await LogLeave(channelName);
    }

    private Task LogJoin(string channelName) =>
        LogCollection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = new ObjectId(),
            ["channel"] = channelName,
            ["type"] = "join",
            ["timestamp"] = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc()
        });

    private Task LogLeave(string channelName) =>
        LogCollection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = new ObjectId(),
            ["channel"] = channelName,
            ["type"] = "leave",
            ["timestamp"] = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc()
        });
}
