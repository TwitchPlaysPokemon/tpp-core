using System;
using System.Collections.Generic;
using System.Threading;
using MongoDB.Driver;
using NUnit.Framework;

namespace Persistence.MongoDB.Tests.Repos
{
    /// <summary>
    /// Base class for tests that need to operate on an actual MongoDB server.
    /// Connects to a local mongod instance running on the default port (27017).
    /// Provides a CreateTemporaryDatabase method for obtaining a unique IMongoDatabase.
    /// Databases created that way get cleaned up after the test method finishes.
    /// </summary>
    public abstract class MongoTestBase
    {
        private static readonly Random Random = new Random();

        private MongoClient _client = null!;
        private readonly List<string> _temporaryDatabases = new List<string>();

        [OneTimeSetUp]
        public void SetUpMongoClient()
        {
            // try to connect to a mongodb running on the default port
            _client = new MongoClient("mongodb://localhost");
            bool success = _client.ListDatabaseNamesAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(5));
            if (!success)
            {
                throw new AssertionException(
                    "Failed to connect to a local MongoDB instance running on the default port. " +
                    "Please start a local MongoDB instance on the default port (27017).");
            }
        }

        [TearDown]
        public void TearDownTempDatabases()
        {
            foreach (string db in _temporaryDatabases)
            {
                _client.DropDatabase(db);
            }
        }

        protected IMongoDatabase CreateTemporaryDatabase()
        {
            string dbName = "testdb-" + Random.Next();
            _temporaryDatabases.Add(dbName);
            return _client.GetDatabase(dbName);
        }
    }
}
