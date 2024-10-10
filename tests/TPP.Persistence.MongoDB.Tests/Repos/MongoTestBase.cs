using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Tests.Repos
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
            try
            {
                CustomSerializers.RegisterAll();

                // Set up MongoDB client settings to connect to replica set
                MongoClientSettings settings = MongoClientSettings
                    .FromConnectionString($"mongodb://localhost:27017/?replicaSet={ReplicaSetName}");
                settings.LinqProvider = LinqProvider.V3;

                // Initialize the client
                _client = new MongoClient(settings);

                // Check connection and list databases to verify replica set mode
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
                var dbNames = _client.ListDatabaseNamesAsync(cancellationToken).Result;

                Console.WriteLine("Successfully connected to MongoDB instance.");
            }
            catch (Exception ex)
            {
                // Log the error details
                Console.WriteLine($"MongoDB connection failed: {ex.Message}");

                // Option to skip the tests if MongoDB is not available
                Assert.Ignore(
                    "MongoDB instance is not available. Skipping integration tests. " +
                    "Ensure MongoDB is running on the default port (27017) and in replica set mode with 'rs0'. " +
                    "Alternatively, run tests with 'dotnet test --filter TestCategory!=IntegrationTest'"
                );
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
