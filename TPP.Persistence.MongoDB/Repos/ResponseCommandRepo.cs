using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TPP.Model;

namespace TPP.Persistence.MongoDB.Repos;

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
        ResponseCommand newCommand = new(command, response);
        ResponseCommand? oldCommand = await Collection.FindOneAndReplaceAsync(
            Builders<ResponseCommand>.Filter.Eq(c => c.Command, command),
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
        DeleteResult deleteOneAsync = await Collection.DeleteOneAsync(c => c.Command == command);
        CommandRemoved?.Invoke(this, command);
        return deleteOneAsync.DeletedCount > 0;
    }
}
