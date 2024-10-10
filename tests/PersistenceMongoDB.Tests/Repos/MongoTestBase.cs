using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;
using Persistence;
using PersistenceMongoDB.Serializers;

namespace PersistenceMongoDB.Tests.Repos
{
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
            try
            {
                MongoClientSettings settings = MongoClientSettings
                    .FromConnectionString($"mongodb://localhost:27017/?replicaSet={ReplicaSetName}");
                settings.LinqProvider = LinqProvider.V3;
                _client = new MongoClient(settings);

                // Attempt to list databases with a timeout to confirm connection
                bool success = _client.ListDatabaseNamesAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(5));
                if (!success)
                {
                    Assert.Ignore("MongoDB instance not available on localhost:27017. Skipping integration tests.");
                }
            }
            catch (Exception ex)
            {
                Assert.Ignore($"Skipping tests due to MongoDB connection failure: {ex.Message}");
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
}
