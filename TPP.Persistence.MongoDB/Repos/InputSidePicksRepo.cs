using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace TPP.Persistence.MongoDB.Repos;

internal class SidePick
{
    [BsonId]
    public string? UserId { get; init; }
    [BsonElement("side")]
    public string? Side { get; init; }
}

public class InputSidePicksRepo : IInputSidePicksRepo
{
    private const string CollectionName = "inputsidepicks";
    private readonly IMongoCollection<SidePick> _collection;

    public InputSidePicksRepo(IMongoDatabase database)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        _collection = database.GetCollection<SidePick>(CollectionName);
    }

    public async Task SetSide(string userId, string? side) =>
        await _collection.FindOneAndUpdateAsync(
            Builders<SidePick>.Filter.Eq(pick => pick.UserId, userId),
            Builders<SidePick>.Update.Set(pick => pick.Side, side),
            new FindOneAndUpdateOptions<SidePick> { IsUpsert = true });

    public async Task<string?> GetSide(string userId) =>
        (await _collection.Find(pick => pick.UserId == userId).FirstOrDefaultAsync())?.Side;

    public async Task ClearAll() => await _collection.DeleteManyAsync(FilterDefinition<SidePick>.Empty);
}
