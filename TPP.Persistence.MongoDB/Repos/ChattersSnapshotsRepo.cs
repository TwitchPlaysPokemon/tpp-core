using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NodaTime;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos;

public class ChattersSnapshotsRepo(IMongoDatabase database) : IChattersSnapshotsRepo, IAsyncInitRepo
{
    public const string CollectionName = "chatters_snapshots";

    public readonly IMongoCollection<ChattersSnapshot> Collection = database.GetCollection<ChattersSnapshot>(CollectionName);

    static ChattersSnapshotsRepo()
    {
        BsonClassMap.RegisterClassMap<ChattersSnapshot>(cm =>
        {
            cm.MapIdProperty(i => i.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(i => i.ChatterNames).SetElementName("chatters");
            cm.MapProperty(i => i.ChatterIds).SetElementName("chatter_ids");
            cm.MapProperty(i => i.Channel).SetElementName("channel");
            cm.MapProperty(i => i.Timestamp).SetElementName("timestamp");
        });
    }

    public async Task InitializeAsync()
    {
        await database.CreateCollectionIfNotExists(CollectionName);
        await Collection.Indexes.CreateManyAsync([
            new CreateIndexModel<ChattersSnapshot>(Builders<ChattersSnapshot>.IndexKeys.Ascending(u => u.Timestamp))
        ]);
    }

    public async Task<ChattersSnapshot> LogChattersSnapshot(
        IImmutableList<string> chatterNames,
        IImmutableList<string> chatterIds,
        string channel,
        Instant timestamp)
    {
        var item = new ChattersSnapshot(string.Empty, chatterNames, chatterIds, timestamp, channel);
        await Collection.InsertOneAsync(item);
        return item;
    }

    public async Task<ChattersSnapshot?> GetRecentChattersSnapshot(Instant from, Instant to) =>
        await Collection.AsQueryable()
            .Where(snapshot => snapshot.Timestamp >= from && snapshot.Timestamp <= to)
            .OrderByDescending(snapshot => snapshot.Timestamp)
            .FirstOrDefaultAsync();
}
