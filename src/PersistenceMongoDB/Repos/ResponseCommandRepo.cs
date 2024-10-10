using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Xml.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Model;
using Persistence;

namespace PersistenceMongoDB.Repos
{
    public class ResponseCommandRepo : IResponseCommandRepo
    {
        public const string CollectionName = "response_commands";

        public readonly IMongoCollection<ResponseCommand> Collection;

        public event EventHandler<ResponseCommand>? CommandInserted;
        public event EventHandler<string>? CommandRemoved;

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

        public async Task<ResponseCommand> UpsertCommand(string command, string response)
        {
            var commandLower = command.ToLower();
            ResponseCommand newCommand = new(commandLower, response);
            ResponseCommand? oldCommand = await Collection.FindOneAndReplaceAsync(
                Builders<ResponseCommand>.Filter.Eq(c => c.Command, commandLower),
                newCommand,
                new FindOneAndReplaceOptions<ResponseCommand>
                {
                    IsUpsert = true,
                    ReturnDocument = ReturnDocument.Before
                });
            if (oldCommand != null)
                CommandRemoved?.Invoke(this, oldCommand.Command);
            CommandInserted?.Invoke(this, newCommand);
            return newCommand;
        }

        public async Task<bool> RemoveCommand(string command)
        {
            var commandLower = command.ToLower();
            DeleteResult deleteOneAsync = await Collection.DeleteOneAsync(c => c.Command == command || c.Command == commandLower);
            CommandRemoved?.Invoke(this, command);
            return deleteOneAsync.DeletedCount > 0;
        }
    }
}
