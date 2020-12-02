using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using Persistence.Models;
using Persistence.MongoDB.Serializers;
using Persistence.Repos;

namespace Persistence.MongoDB.Repos
{
    public class CommandLogger : ICommandLogger
    {
        private const string CollectionName = "commandlog";

        public readonly IMongoCollection<CommandLog> Collection;

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

        private readonly IClock _clock;

        public CommandLogger(IMongoDatabase database, IClock clock)
        {
            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<CommandLog>(CollectionName);
            _clock = clock;
        }

        public async Task<CommandLog> Log(User user, string command, IImmutableList<string> args, string? response)
        {
            var log = new CommandLog(
                string.Empty, user.Id, command, args.ToImmutableList(), _clock.GetCurrentInstant(), response);
            await Collection.InsertOneAsync(log);
            Debug.Assert(log.Id.Length > 0, "The MongoDB driver injected a generated ID");
            return log;
        }
    }
}
