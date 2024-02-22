using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NodaTime;

namespace TPP.Persistence.MongoDB.Repos;

public class CoStreamChannelsRepo : ICoStreamChannelsRepo
{
    private const string CollectionName = "costream_channels";
    private const string LogCollectionName = "costream_channels_log";

    public readonly IMongoCollection<BsonDocument> Collection;
    public readonly IMongoCollection<BsonDocument> LogCollection;

    public CoStreamChannelsRepo(IMongoDatabase database)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<BsonDocument>(CollectionName);
        LogCollection = database.GetCollection<BsonDocument>(LogCollectionName);
    }

    public Task<bool> IsJoined(string channelName) =>
        Collection.Find(doc => doc["_id"] == channelName.ToLower()).AnyAsync();

    public async Task<string?> GetChannelImageUrl(string channelName)
    {
        BsonDocument? doc = await Collection.Find(doc => doc["_id"] == channelName.ToLower()).FirstOrDefaultAsync();
        return doc?["profile_image_url"].AsString;
    }

    public async Task<IImmutableSet<string>> GetJoinedChannels() =>
        (await Collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())
        .Select(doc => doc["_id"].AsString).ToImmutableHashSet();

    public async Task Add(string channelName, string? profileImageUrl)
    {
        await Collection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = channelName.ToLower(),
            ["profile_image_url"] = profileImageUrl
        });
        await LogJoin(channelName.ToLower(), profileImageUrl);
    }

    public async Task Remove(string channelName)
    {
        await Collection.DeleteOneAsync(doc => doc["_id"] == channelName.ToLower());
        await LogLeave(channelName.ToLower());
    }

    private Task LogJoin(string channelName, string? profileImageUrl) =>
        LogCollection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = new ObjectId(),
            ["channel"] = channelName,
            ["profile_image_url"] = profileImageUrl,
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
