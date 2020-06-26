using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Persistence.MongoDB
{
    public static class MongoDatabaseExtensions
    {
        public static async Task CreateCollectionIfNotExists(
            this IMongoDatabase database,
            string name,
            CreateCollectionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var filter = new ListCollectionNamesOptions { Filter = new BsonDocument("name", name) };
            bool dbExists = await (await database.ListCollectionNamesAsync(filter, cancellationToken))
                .AnyAsync(cancellationToken: cancellationToken);
            if (!dbExists) await database.CreateCollectionAsync(name, options, cancellationToken);
        }
    }
}
