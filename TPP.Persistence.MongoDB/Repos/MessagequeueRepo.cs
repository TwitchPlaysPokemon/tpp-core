using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos
{
    public class MessagequeueRepo : IMessagequeueRepo
    {
        public const string CollectionName = "messagequeue";

        public readonly IMongoCollection<MessagequeueItem> Collection;

        static MessagequeueRepo()
        {
            BsonClassMap.RegisterClassMap<MessagequeueItem>(cm =>
            {
                cm.MapIdProperty(i => i.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(ObjectIdAsStringSerializer.Instance);
                cm.MapProperty(i => i.IrcLine).SetElementName("ircline");
            });
        }

        public MessagequeueRepo(IMongoDatabase database)
        {
            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<MessagequeueItem>(CollectionName);
        }

        public async Task<MessagequeueItem> EnqueueMessage(string ircLine)
        {
            var item = new MessagequeueItem(string.Empty, ircLine);
            await Collection.InsertOneAsync(item);
            return item;
        }
    }
}
