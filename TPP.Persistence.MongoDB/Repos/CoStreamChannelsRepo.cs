using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NodaTime;

namespace TPP.Persistence.MongoDB.Repos;

public class CoStreamChannelsRepo(IMongoDatabase database) : ICoStreamChannelsRepo
{
    private const string CollectionName = "costream_channels";
    private const string LogCollectionName = "costream_channels_log";

    public readonly IMongoCollection<BsonDocument> Collection = database.GetCollection<BsonDocument>(CollectionName);
    public readonly IMongoCollection<BsonDocument> LogCollection = database.GetCollection<BsonDocument>(LogCollectionName);

    public Task<bool> IsJoined(string channelId) =>
        Collection.Find(doc => doc["_id"] == channelId).AnyAsync();

    public async Task<string?> GetChannelImageUrl(string channelId)
    {
        BsonDocument? doc = await Collection.Find(doc => doc["_id"] == channelId).FirstOrDefaultAsync();
        return doc?["profile_image_url"].AsString;
    }

    public async Task<IImmutableSet<string>> GetJoinedChannels() =>
        (await Collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())
        .Select(doc => doc["_id"].AsString).ToImmutableHashSet();

    public async Task Add(string channelId, string? profileImageUrl)
    {
        await Collection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = channelId,
            ["profile_image_url"] = profileImageUrl
        });
        await LogJoin(channelId, profileImageUrl);
    }

    public async Task Remove(string channelId)
    {
        await Collection.DeleteOneAsync(doc => doc["_id"] == channelId);
        await LogLeave(channelId);
    }

    private Task LogJoin(string channelId, string? profileImageUrl) =>
        LogCollection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = new ObjectId(),
            ["channel"] = channelId,
            ["profile_image_url"] = profileImageUrl,
            ["type"] = "join",
            ["timestamp"] = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc()
        });

    private Task LogLeave(string channelId) =>
        LogCollection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = new ObjectId(),
            ["channel"] = channelId,
            ["type"] = "leave",
            ["timestamp"] = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc()
        });
}
