using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TPP.Persistence.MongoDB.Repos;

public class RunCounterRepo : IRunCounterRepo
{
    private const string RunSpecificCounterCollectionName = "button_presses";
    private const string GlobalCounterCollectionName = "misc";
    private const string GlobalButtonPressesCollId = "button_presses";
    private const string ButtonPressesFieldName = "presses";

    public readonly IMongoCollection<BsonDocument> RunSpecificCounterCollection;
    public readonly IMongoCollection<BsonDocument> GlobalCounterCollection;

    public RunCounterRepo(IMongoDatabase database)
    {
        database.CreateCollectionIfNotExists(RunSpecificCounterCollectionName).Wait();
        database.CreateCollectionIfNotExists(GlobalCounterCollectionName).Wait();
        RunSpecificCounterCollection = database.GetCollection<BsonDocument>(RunSpecificCounterCollectionName);
        GlobalCounterCollection = database.GetCollection<BsonDocument>(GlobalCounterCollectionName);
    }

    public async Task<long> Increment(int? runNumber, int incrementBy = 1)
    {
        BsonDocument globalCounter = await GlobalCounterCollection.FindOneAndUpdateAsync(
            Builders<BsonDocument>.Filter.Eq(doc => doc["_id"], GlobalButtonPressesCollId),
            Builders<BsonDocument>.Update.Inc(doc => doc[ButtonPressesFieldName], incrementBy),
            new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After }
        );
        long counter = globalCounter[ButtonPressesFieldName].ToInt64();
        if (runNumber != null)
        {
            BsonDocument runSpecificCounter = await RunSpecificCounterCollection.FindOneAndUpdateAsync(
                Builders<BsonDocument>.Filter.Eq(doc => doc["_id"], runNumber.Value),
                Builders<BsonDocument>.Update.Inc(doc => doc[ButtonPressesFieldName], incrementBy),
                new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After }
            );
            counter = runSpecificCounter[ButtonPressesFieldName].ToInt64();
        }
        return counter;
    }

    public async Task<long> Get(int? runNumber)
    {
        IFindFluent<BsonDocument, BsonDocument>? query =
            runNumber == null
                ? GlobalCounterCollection.Find(doc => doc["_id"] == GlobalButtonPressesCollId)
                : RunSpecificCounterCollection.Find(doc => doc["_id"] == runNumber.Value);
        BsonDocument? counter = await query.FirstOrDefaultAsync();
        return counter == null ? 0 : counter[ButtonPressesFieldName].ToInt64();
    }
}
