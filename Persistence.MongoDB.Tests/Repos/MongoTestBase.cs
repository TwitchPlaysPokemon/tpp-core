using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

        private          string       _tempDbPath         = null!;
        private          Process      _mongod             = null!;
        private          MongoClient  _client             = null!;
        private readonly List<string> _temporaryDatabases = new List<string>();

        [OneTimeSetUp]
        public void SetUpMongodProcess()
        {
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
                    "Failed to start mongod instance. Is MongoDB server installed and its /bin on the PATH?", ex);
            }
            _client = new MongoClient($"mongodb://localhost:{port}");
        }

        [OneTimeTearDown]
        public void TearDownMongodProcess()
        {
            _mongod.Kill();
            _mongod.WaitForExit();
            Directory.Delete(_tempDbPath, recursive: true);
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
            string dbName = "db-" + Random.Next();
            _temporaryDatabases.Add(dbName);
            return _client.GetDatabase(dbName);
        }
    }
}
