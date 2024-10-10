using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using Model;
using PersistenceMongoDB.Serializers;

namespace PersistenceMongoDB.Repos;

public class OutgoingMessagequeueRepo : IOutgoingMessagequeueRepo
{
    public const string CollectionName = "messagequeue";

    public readonly IMongoCollection<OutgoingMessagequeueItem> Collection;

    static OutgoingMessagequeueRepo()
    {
        BsonClassMap.RegisterClassMap<OutgoingMessagequeueItem>(cm =>
        {
            cm.MapIdProperty(i => i.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(i => i.IrcLine).SetElementName("ircline");
        });
    }

    public OutgoingMessagequeueRepo(IMongoDatabase database)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<OutgoingMessagequeueItem>(CollectionName);
    }

    public async Task<OutgoingMessagequeueItem> EnqueueMessage(string ircLine)
    {
        var item = new OutgoingMessagequeueItem(string.Empty, ircLine);
        await Collection.InsertOneAsync(item);
        return item;
    }
}
