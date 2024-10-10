using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using Model;
using Persistence;
using PersistenceMongoDB.Serializers;

namespace PersistenceMongoDB.Repos;

public class ChattersSnapshotsRepo : IChattersSnapshotsRepo
{
    public const string CollectionName = "chatters_snapshots";

    public readonly IMongoCollection<ChattersSnapshot> Collection;

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

    public ChattersSnapshotsRepo(IMongoDatabase database)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<ChattersSnapshot>(CollectionName);
        InitIndexes();
    }

    private void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<ChattersSnapshot>(Builders<ChattersSnapshot>.IndexKeys.Ascending(u => u.Timestamp))
        });
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
}
