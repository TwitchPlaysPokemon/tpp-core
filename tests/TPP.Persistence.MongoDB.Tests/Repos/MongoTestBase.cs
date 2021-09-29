using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Tests.Repos;

/// <summary>
/// Base class for tests that need to operate on an actual MongoDB server.
/// Connects to a local mongod instance running on the default port (27017).
/// Provides a CreateTemporaryDatabase method for obtaining a unique IMongoDatabase.
/// Databases created that way get cleaned up while the test class is being torn down.
/// </summary>
[Category("IntegrationTest")]
public abstract class MongoTestBase
{
    private const string ReplicaSetName = "rs0";
    private static readonly Random Random = new Random();

    private MongoClient _client = null!;
    private readonly List<string> _temporaryDatabases = new List<string>();

    [OneTimeSetUp]
    public void SetUpMongoClient()
    {
        CustomSerializers.RegisterAll();
        // try to connect to a mongodb running on the default port
        _client = new MongoClient($"mongodb://localhost:27017/?replicaSet={ReplicaSetName}");
        bool success = _client.ListDatabaseNamesAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(5));
        if (!success)
        {
            throw new AssertionException(
                "Failed to connect to a local MongoDB instance running on the default port. " +
                "Please start a local MongoDB instance on the default port (27017), " +
                $"and make sure it is in replica set mode with a replica set named '{ReplicaSetName}'. " +
                "Alternatively, skip these tests using 'dotnet test --filter TestCategory!=IntegrationTest'");
        }
    }

    [OneTimeTearDown]
    public void TearDownTempDatabases()
    {
        Task.WhenAll(_temporaryDatabases.Select(db => _client.DropDatabaseAsync(db))).Wait();
    }

    protected IMongoDatabase CreateTemporaryDatabase()
    {
        string dbName = "testdb-" + Random.Next();
        _temporaryDatabases.Add(dbName);
        return _client.GetDatabase(dbName);
    }
}
