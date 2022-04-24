using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence.MongoDB.Repos;

public class InputSidePicksRepo : IInputSidePicksRepo
{
    private const string CollectionName = "inputsidepicks";
    private readonly IMongoCollection<SidePick> _collection;
    private readonly IClock _clock;

    public InputSidePicksRepo(IMongoDatabase database, IClock clock)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        _collection = database.GetCollection<SidePick>(CollectionName);
        _clock = clock;
    }

    static InputSidePicksRepo()
    {
        BsonClassMap.RegisterClassMap<SidePick>(cm =>
        {
            cm.MapIdField(p => p.UserId);
            cm.MapProperty(p => p.Side).SetElementName("side");
            cm.MapProperty(p => p.PickedAt).SetElementName("picked_at");
        });
    }

    public async Task SetSide(string userId, string? side) =>
        await _collection.FindOneAndUpdateAsync(
            Builders<SidePick>.Filter.Eq(pick => pick.UserId, userId),
            Builders<SidePick>.Update
                .Set(pick => pick.Side, side)
                .Set(pick => pick.PickedAt, _clock.GetCurrentInstant()),
            new FindOneAndUpdateOptions<SidePick> { IsUpsert = true });

    public async Task<SidePick?> GetSidePick(string userId) =>
        await _collection.Find(pick => pick.UserId == userId).FirstOrDefaultAsync();

    public async Task ClearAll() => await _collection.DeleteManyAsync(FilterDefinition<SidePick>.Empty);
}
