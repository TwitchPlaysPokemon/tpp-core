using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NUnit.Framework;
using Persistence.Models;
using Persistence.MongoDB.Repos;
using Persistence.Repos;

namespace Persistence.MongoDB.Tests.Repos
{
    internal class TestUser
    {
        [BsonId] public string Id { get; } = Guid.NewGuid().ToString();
        [BsonElement] public int Money { get; set; }
        public override string ToString() => Id;
    }

    [Category("IntegrationTest")]
    public class BankTest : MongoTestBase
    {
        private IMongoDatabase _database = null!;
        private IMongoCollection<TestUser> _usersCollection = null!;

        private IBank<TestUser> _bank = null!;

        [SetUp]
        public void SetUp()
        {
            _database = CreateTemporaryDatabase();
            _bank = new Bank<TestUser>(
                _database,
                "users",
                "transactionLog",
                user => user.Money,
                user => user.Id
            );
            _usersCollection = _database.GetCollection<TestUser>("users");
        }

        [Test]
        public async Task TestUpdate()
        {
            var user = new TestUser {Money = 10};
            await _usersCollection.InsertOneAsync(user);

            var transaction = new Transaction<TestUser>(user, 1, TransactionType.Test);
            TransactionLog log = await _bank.PerformTransaction(transaction);

            Assert.AreEqual(10, log.OldBalance);
            Assert.AreEqual(11, log.NewBalance);
            Assert.AreEqual(1, log.Change);
            Assert.AreEqual(TransactionType.Test, log.Type);
            TestUser userAfter = await _usersCollection.Find(u => u.Id == user.Id).FirstAsync();
            Assert.AreEqual(11, userAfter.Money);
            Assert.AreEqual(11, user.Money); // new balance value was injected into existing object as well
        }

        [Test]
        public async Task TestAbortStaleUpdate()
        {
            var user = new TestUser {Money = 10};
            await _usersCollection.InsertOneAsync(user);
            user.Money = 5; // object is stale, amount does not match database

            var transaction = new Transaction<TestUser>(user, 1, TransactionType.Test);
            InvalidOperationException failure = Assert.ThrowsAsync<InvalidOperationException>(
                () => _bank.PerformTransaction(transaction));

            Assert.AreEqual("tried to perform transaction with stale user data", failure.Message);

            TestUser userAfter = await _usersCollection.Find(u => u.Id == user.Id).FirstAsync();
            Assert.AreEqual(10, userAfter.Money);
            Assert.AreEqual(5, user.Money); // no new balance was injected
        }

        [Test]
        public void TestUserNotFound()
        {
            var user = new TestUser {Money = 10}; // user not persisted

            var transaction = new Transaction<TestUser>(user, 1, TransactionType.Test);
            UserNotFoundException<TestUser> userNotFound = Assert.ThrowsAsync<UserNotFoundException<TestUser>>(
                () => _bank.PerformTransaction(transaction));
            Assert.AreEqual(user, userNotFound.User);
        }

        [Test]
        public async Task TestMultipleTransactionsTransactional()
        {
            TestUser knownUser = new TestUser {Money = 10};
            TestUser unknownUser = new TestUser {Money = 20};
            await _usersCollection.InsertOneAsync(knownUser);

            UserNotFoundException<TestUser> userNotFound = Assert.ThrowsAsync<UserNotFoundException<TestUser>>(() =>
                _bank.PerformTransactions(new[]
                {
                    new Transaction<TestUser>(knownUser, 3, TransactionType.Test),
                    new Transaction<TestUser>(unknownUser, -3, TransactionType.Test)
                })
            );

            Assert.AreEqual(unknownUser, userNotFound.User);
            // ensure neither user's balance was modified
            Assert.AreEqual(10, knownUser.Money);
            Assert.AreEqual(20, unknownUser.Money);
            TestUser knownUserAfterwards = await _usersCollection.Find(u => u.Id == knownUser.Id).FirstAsync();
            Assert.AreEqual(10, knownUserAfterwards.Money);
        }

        [Test]
        public async Task TestReserveMoney()
        {
            TestUser user = new TestUser {Money = 10};
            TestUser otherUser = new TestUser {Money = 20};
            await _usersCollection.InsertManyAsync(new[] {user, otherUser});
            Task<int> Checker(TestUser u) => Task.FromResult(u == user ? 1 : 0);

            Assert.AreEqual(10, await _bank.GetAvailableMoney(user));
            Assert.AreEqual(20, await _bank.GetAvailableMoney(otherUser));
            _bank.AddReservedMoneyChecker(Checker);
            Assert.AreEqual(9, await _bank.GetAvailableMoney(user));
            Assert.AreEqual(20, await _bank.GetAvailableMoney(otherUser));
            _bank.RemoveReservedMoneyChecker(Checker);
            Assert.AreEqual(10, await _bank.GetAvailableMoney(user));
            Assert.AreEqual(20, await _bank.GetAvailableMoney(otherUser));
        }

        [Test]
        public void TestGetMoneyUnknownUser()
        {
            TestUser unknownUser = new TestUser {Money = 0}; // not persisted
            UserNotFoundException<TestUser> userNotFound = Assert.ThrowsAsync<UserNotFoundException<TestUser>>(
                () => _bank.GetAvailableMoney(unknownUser));
            Assert.AreEqual(unknownUser, userNotFound.User);
        }

        [Test]
        public async Task TestDatabaseSerialization()
        {
            TestUser user = new TestUser {Money = 10};
            await _usersCollection.InsertOneAsync(user);
            List<int> list = new List<int> {1, 2, 3};
            Dictionary<string, bool> dictionary = new Dictionary<string, bool> {["yes"] = true, ["no"] = false};
            await _bank.PerformTransaction(new Transaction<TestUser>(user, 1, TransactionType.Test,
                new Dictionary<string, object?>
                {
                    ["null_field"] = null,
                    ["int_field"] = 42,
                    ["string_field"] = "foo",
                    ["list_field"] = list,
                    ["dictionary_field"] = dictionary
                }));
            IMongoCollection<BsonDocument> transactionLogCollection =
                _database.GetCollection<BsonDocument>("transactionLog");
            BsonDocument log = await transactionLogCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();

            Assert.IsInstanceOf<BsonObjectId>(log["_id"]);
            Assert.AreEqual(BsonString.Create(user.Id), log["user"]);
            Assert.AreEqual(BsonInt32.Create(1), log["change"]);
            Assert.AreEqual(BsonInt32.Create(10), log["old_balance"]);
            Assert.AreEqual(BsonInt32.Create(11), log["new_balance"]);
            Assert.GreaterOrEqual(DateTime.UtcNow, log["timestamp"].ToUniversalTime());
            Assert.LessOrEqual(DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)), log["timestamp"].ToUniversalTime());
            Assert.AreEqual(BsonString.Create("test"), log["type"]);
            Assert.AreEqual(BsonNull.Value, log["null_field"]);
            Assert.AreEqual(BsonInt32.Create(42), log["int_field"]);
            Assert.AreEqual(BsonString.Create("foo"), log["string_field"]);
            Assert.AreEqual(BsonArray.Create(list), log["list_field"]);
            Assert.AreEqual(BsonDocument.Create(dictionary), log["dictionary_field"]);
        }

        [Test]
        public async Task TestDeserializeWithNullTransactionType()
        {
            // for some reason the "type" field is sometimes missing in the existing database.
            // that should just get mapped to "Unknown".
            const string id = "590df61373b975210006fcdf";
            DateTime dateTime = DateTime.SpecifyKind(DateTime.Parse("2017-05-06T16:13:07.314Z"), DateTimeKind.Utc);
            IMongoCollection<BsonDocument> bsonTransactionLogCollection =
                _database.GetCollection<BsonDocument>("transactionLog");
            await bsonTransactionLogCollection.InsertOneAsync(BsonDocument.Create(new Dictionary<string, object?>()
            {
                ["_id"] = ObjectId.Parse(id),
                ["user"] = "137272735",
                ["change"] = -9,
                ["timestamp"] = dateTime,
                ["old_balance"] = 25,
                ["new_balance"] = 16,
                ["match"] = 35510,
            }));

            IMongoCollection<TransactionLog> transactionLogCollection =
                _database.GetCollection<TransactionLog>("transactionLog");
            TransactionLog log = await transactionLogCollection.Find(t => t.Id == id).FirstAsync();

            Assert.AreEqual(id, log.Id);
            Assert.AreEqual("137272735", log.UserId);
            Assert.AreEqual(-9, log.Change);
            Assert.AreEqual(25, log.OldBalance);
            Assert.AreEqual(16, log.NewBalance);
            Assert.AreEqual(dateTime, log.CreatedAt);
            Assert.AreEqual(TransactionType.Unknown, log.Type);
            Assert.AreEqual(new Dictionary<string, object?> {["match"] = 35510}, log.AdditionalData);
        }
    }
}
