using System.Diagnostics;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos;

public class InputLogRepo(IMongoDatabase database) : IInputLogRepo, IAsyncInitRepo
{
    private const string CollectionName = "inputlog";

    public readonly IMongoCollection<InputLog> Collection = database.GetCollection<InputLog>(CollectionName);

    static InputLogRepo()
    {
        BsonClassMap.RegisterClassMap<InputLog>(cm =>
        {
            cm.MapIdProperty(b => b.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(b => b.UserId).SetElementName("user");
            cm.MapProperty(b => b.Message).SetElementName("message");
            cm.MapProperty(b => b.Timestamp).SetElementName("timestamp");
        });
    }

    public async Task InitializeAsync()
    {
        await database.CreateCollectionIfNotExists(CollectionName);
        await Collection.Indexes.CreateManyAsync([
            new CreateIndexModel<InputLog>(Builders<InputLog>.IndexKeys.Ascending(u => u.Timestamp))
        ]);
    }

    public async Task<InputLog> LogInput(string userId, string message, Instant timestamp)
    {
        var inputLog = new InputLog(string.Empty, userId, message, timestamp);
        await Collection.InsertOneAsync(inputLog);
        Debug.Assert(inputLog.Id.Length > 0, "The MongoDB driver injected a generated ID");
        return inputLog;
    }
}
