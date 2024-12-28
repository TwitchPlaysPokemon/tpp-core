using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos
{
    public class MessagelogRepo(IMongoDatabase database) : IMessagelogRepo, IAsyncInitRepo
    {
        public const string CollectionName = "messagelog";

        public readonly IMongoCollection<Messagelog> Collection = database.GetCollection<Messagelog>(CollectionName);

        static MessagelogRepo()
        {
            BsonClassMap.RegisterClassMap<Messagelog>(cm =>
            {
                cm.MapIdProperty(i => i.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(ObjectIdAsStringSerializer.Instance);
                cm.MapProperty(i => i.UserId).SetElementName("user");
                cm.MapProperty(i => i.Message).SetElementName("message");
                cm.MapProperty(i => i.IrcLine).SetElementName("ircline");
                cm.MapProperty(i => i.Timestamp).SetElementName("timestamp");
            });
        }

        public async Task InitializeAsync()
        {
            await database.CreateCollectionIfNotExists(CollectionName);
        }

        public async Task<Messagelog> LogChat(string userId, string ircLine, string message, Instant timestamp)
        {
            var item = new Messagelog(string.Empty, ircLine, userId, message, timestamp);
            await Collection.InsertOneAsync(item);
            return item;
        }
    }
}
