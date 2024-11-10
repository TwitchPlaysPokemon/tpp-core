using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NUnit.Framework;
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
    private static readonly Random Random = new();

    private MongoClient _client = null!;
    private readonly List<string> _temporaryDatabases = [];

    [OneTimeSetUp]
    public void SetUpMongoClient()
    {
        CustomSerializers.RegisterAll();
        // try to connect to a mongodb running on the default port
        MongoClientSettings settings = MongoClientSettings
            .FromConnectionString($"mongodb://localhost:27017/?replicaSet={ReplicaSetName}");
        _client = new MongoClient(settings);
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
        // ReSharper disable once AccessToDisposedClosure : task is Wait()-ed on before client gets disposed.
        IEnumerable<Task> dropTasks = _temporaryDatabases.Select(db => _client.DropDatabaseAsync(db));
        Task.WhenAll(dropTasks).Wait();
        _client.Dispose();
    }

    protected IMongoDatabase CreateTemporaryDatabase()
    {
        string dbName = "testdb-" + Random.Next();
        _temporaryDatabases.Add(dbName);
        return _client.GetDatabase(dbName);
    }
}
