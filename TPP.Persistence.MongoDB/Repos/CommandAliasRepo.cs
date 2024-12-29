using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TPP.Model;

namespace TPP.Persistence.MongoDB.Repos;

public class CommandAliasRepo(IMongoDatabase database) : ICommandAliasRepo
{
    public const string CollectionName = "command_aliases";

    public readonly IMongoCollection<CommandAlias> Collection = database.GetCollection<CommandAlias>(CollectionName);

    public event EventHandler<CommandAlias>? AliasInserted;
    public event EventHandler<string>? AliasRemoved;

    static CommandAliasRepo()
    {
        BsonClassMap.RegisterClassMap<CommandAlias>(cm =>
        {
            cm.MapIdProperty(i => i.Alias);
            cm.MapProperty(i => i.TargetCommand).SetElementName("target_command");
            cm.MapProperty(i => i.FixedArgs).SetElementName("fixed_arguments")
                .SetDefaultValue(Array.Empty<string>());
        });
    }

    public async Task<IImmutableList<CommandAlias>> GetAliases() =>
        (await Collection.Find(FilterDefinition<CommandAlias>.Empty).ToListAsync()).ToImmutableList();

    public async Task<CommandAlias> UpsertAlias(string alias, string targetCommand, string[] fixedArgs)
    {
        if (targetCommand.IndexOf(' ') != -1)
            throw new ArgumentException(nameof(targetCommand) + " must not contain spaces");
        var aliasLower = alias.ToLower();
        CommandAlias newAlias = new(aliasLower, targetCommand, fixedArgs);
        CommandAlias? oldAlias = await Collection.FindOneAndReplaceAsync(
            Builders<CommandAlias>.Filter.Eq(c => c.Alias, aliasLower),
            newAlias,
            new FindOneAndReplaceOptions<CommandAlias>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.Before
            });
        if (oldAlias != null)
            AliasRemoved?.Invoke(this, oldAlias.Alias);
        AliasInserted?.Invoke(this, newAlias);
        return newAlias;
    }

    public async Task<bool> RemoveAlias(string alias)
    {
        var aliasLower = alias.ToLower();
        DeleteResult deleteOneAsync = await Collection.DeleteOneAsync(c => c.Alias == alias || c.Alias == aliasLower);
        AliasRemoved?.Invoke(this, alias);
        return deleteOneAsync.DeletedCount > 0;
    }
}
