using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using Model;
using PersistenceMongoDB.Serializers;

namespace PersistenceMongoDB.Repos;

public class TransmutationLogRepo : ITransmutationLogRepo
{
    public const string CollectionName = "transmutationlog";

    public readonly IMongoCollection<TransmutationLog> Collection;

    static TransmutationLogRepo()
    {
        BsonClassMap.RegisterClassMap<TransmutationLog>(cm =>
        {
            cm.MapIdProperty(b => b.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(b => b.UserId).SetElementName("user");
            cm.MapProperty(b => b.Timestamp).SetElementName("timestamp");
            cm.MapProperty(b => b.Cost).SetElementName("cost");
            cm.MapProperty(b => b.InputBadges).SetElementName("input_badges")
                .SetSerializer(ObjectIdListAsStringSerializer.Instance);
            cm.MapProperty(b => b.OutputBadge).SetElementName("output_badge")
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
        });
    }

    public TransmutationLogRepo(IMongoDatabase database)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<TransmutationLog>(CollectionName);
    }

    public async Task<TransmutationLog> Log(
        string userId, Instant timestamp, int cost, IReadOnlyList<string> inputBadges, string outputBadge)
    {
        var modLog = new TransmutationLog(string.Empty, userId, timestamp, cost, inputBadges, outputBadge);
        await Collection.InsertOneAsync(modLog);
        return modLog;
    }
}
