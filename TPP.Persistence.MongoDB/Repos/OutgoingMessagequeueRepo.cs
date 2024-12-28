using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos;

public class OutgoingMessagequeueRepo(IMongoDatabase database) : IOutgoingMessagequeueRepo, IAsyncInitRepo
{
    public const string CollectionName = "messagequeue";

    public readonly IMongoCollection<OutgoingMessagequeueItem> Collection = database.GetCollection<OutgoingMessagequeueItem>(CollectionName);

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

    public async Task InitializeAsync()
    {
        await database.CreateCollectionIfNotExists(CollectionName);
    }

    public async Task<OutgoingMessagequeueItem> EnqueueMessage(string ircLine)
    {
        var item = new OutgoingMessagequeueItem(string.Empty, ircLine);
        await Collection.InsertOneAsync(item);
        return item;
    }
}
