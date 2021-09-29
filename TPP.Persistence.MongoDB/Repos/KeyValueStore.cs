namespace TPP.Persistence.MongoDB.Repos;

public class KeyValueStore : IKeyValueStore
{
    private const string CollectionName = "misc";

    public readonly IMongoCollection<BsonDocument> Collection;

    public KeyValueStore(IMongoDatabase database)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<BsonDocument>(CollectionName);
    }

    public async Task<T?> Get<T>(string key) =>
        await Collection.Find(doc => doc["_id"] == key).As<T>().FirstOrDefaultAsync();

    public async Task Set<T>(string key, T value) =>
        await Collection.ReplaceOneAsync(doc => doc["_id"] == key, value.ToBsonDocument(),
            new ReplaceOptions { IsUpsert = true });

    public async Task Delete<T>(string key) =>
        await Collection.DeleteOneAsync(doc => doc["_id"] == key);
}
