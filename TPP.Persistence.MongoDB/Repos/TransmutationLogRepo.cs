using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos;

public class TransmutationLogRepo(IMongoDatabase database) : ITransmutationLogRepo, IAsyncInitRepo
{
    public const string CollectionName = "transmutationlog";

    public readonly IMongoCollection<TransmutationLog> Collection = database.GetCollection<TransmutationLog>(CollectionName);

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

    public async Task InitializeAsync()
    {
        await Collection.Indexes.CreateManyAsync([
            new CreateIndexModel<TransmutationLog>(Builders<TransmutationLog>.IndexKeys.Ascending(u => u.InputBadges)),
        ]);
    }

    public async Task<TransmutationLog> Log(
        string userId, Instant timestamp, int cost, IReadOnlyList<string> inputBadges, string outputBadge)
    {
        var modLog = new TransmutationLog(string.Empty, userId, timestamp, cost, inputBadges, outputBadge);
        await Collection.InsertOneAsync(modLog);
        return modLog;
    }
}
