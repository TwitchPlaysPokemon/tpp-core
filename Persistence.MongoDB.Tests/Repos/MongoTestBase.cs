using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MongoDB.Driver;
using NUnit.Framework;

namespace Persistence.MongoDB.Tests.Repos
{
    /// <summary>
    /// Base class for tests that need to operate on an actual MongoDB server.
    /// Starts a mongod process for the duration of the test class execution.
    /// Provides a CreateTemporaryDatabase method for obtaining a unique IMongoDatabase.
    /// Databases created that way get cleaned up after the test method finishes.
    /// </summary>
    public abstract class MongoTestBase
    {
        private static readonly Random Random = new Random();

        private string? _tempDbPath;
        private Process? _mongod;
        private MongoClient _client = null!;
        private readonly List<string> _temporaryDatabases = new List<string>();

        [OneTimeSetUp]
        public void SetUpMongoClient()
        {
            // first try to connect to a mongodb running on the default port (e.g. CI or DEV environment)
            _client = new MongoClient("mongodb://localhost");
            bool success = _client.ListDatabaseNamesAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(5));
            if (success)
            {
                // OK! just use that!
                return;
            }
            // try to start our own instance instead
            _tempDbPath = Path.GetTempPath() + "mongo-temp-" + Random.Next();
            Directory.CreateDirectory(_tempDbPath);
            int port = Random.Next(20000, 60000);
            try
            {
                _mongod = Process.Start("mongod", arguments: $"--dbpath \"{_tempDbPath}\" --port {port}");
            }
            catch (Win32Exception ex)
            {
                throw new AssertionException(
                    "Failed to either connect to a local MongoDB instance running on the default port, " +
                    "or to start a custom mongod instance. " +
                    "Is MongoDB running or the server software installed and its /bin on the PATH?", ex);
            }
            _client = new MongoClient($"mongodb://localhost:{port}");
        }

        [OneTimeTearDown]
        public void TearDownMongoClient()
        {
            _mongod?.Kill();
            _mongod?.WaitForExit();
            if (_tempDbPath != null)
            {
                Directory.Delete(_tempDbPath, recursive: true);
            }
            _mongod = null;
            _tempDbPath = null;
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
