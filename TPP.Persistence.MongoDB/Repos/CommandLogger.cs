using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos;

public class CommandLogger(IMongoDatabase database, IClock clock) : ICommandLogger
{
    private const string CollectionName = "commandlog";

    public readonly IMongoCollection<CommandLog> Collection = database.GetCollection<CommandLog>(CollectionName);

    static CommandLogger()
    {
        BsonClassMap.RegisterClassMap<CommandLog>(cm =>
        {
            cm.MapIdProperty(b => b.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(b => b.UserId).SetElementName("user");
            cm.MapProperty(b => b.Command).SetElementName("command");
            cm.MapProperty(b => b.Args).SetElementName("args");
            cm.MapProperty(b => b.Timestamp).SetElementName("timestamp");
            cm.MapProperty(b => b.Response).SetElementName("response");
        });
    }

    public async Task<CommandLog> Log(string userId, string command, IImmutableList<string> args, string? response)
    {
        var log = new CommandLog(
            string.Empty, userId, command, args, clock.GetCurrentInstant(), response);
        await Collection.InsertOneAsync(log);
        return log;
    }
}
