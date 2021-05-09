using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

namespace TPP.Persistence.MongoDB.Repos
{
    public class ResponseCommandRepo : IResponseCommandRepo
    {
        public const string CollectionName = "response_commands";

        public readonly IMongoCollection<ResponseCommand> Collection;

        static ResponseCommandRepo()
        {
            BsonClassMap.RegisterClassMap<ResponseCommand>(cm =>
            {
                cm.MapIdProperty(i => i.Command);
                cm.MapProperty(i => i.Response).SetElementName("response");
            });
        }

        public ResponseCommandRepo(IMongoDatabase database)
        {
            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<ResponseCommand>(CollectionName);
        }

        public async Task<IImmutableList<ResponseCommand>> GetCommands() =>
            (await Collection.Find(FilterDefinition<ResponseCommand>.Empty).ToListAsync()).ToImmutableList();

        public async Task<ResponseCommand> UpsertCommand(string command, string response) =>
            await Collection.FindOneAndReplaceAsync(
                Builders<ResponseCommand>.Filter.Eq(c => c.Command, command),
                new ResponseCommand(command, response),
                new FindOneAndReplaceOptions<ResponseCommand>
                    { IsUpsert = true, ReturnDocument = ReturnDocument.After });

        public async Task<bool> RemoveCommand(string command) =>
            (await Collection.DeleteOneAsync(c => c.Command == command)).DeletedCount > 0;
    }
}
