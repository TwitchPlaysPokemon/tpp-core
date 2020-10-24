using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;
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

    internal class MockClock : IClock
    {
        public Instant FixedCurrentInstant = Instant.FromUnixTimeSeconds(1234567890);
        public Instant GetCurrentInstant() => FixedCurrentInstant;
    }

    [Category("IntegrationTest")]
    [Parallelizable(ParallelScope.All)]
    public class BankTest : MongoTestBase
    {
        private (IBank<TestUser>, IMongoCollection<TestUser>) CreateDbObjects(IClock clock)
        {
            IMongoDatabase database = CreateTemporaryDatabase();
            var bank = new Bank<TestUser>(
                database: database,
                currencyCollectionName: "users",
                transactionLogCollectionName: "transactionLog",
                currencyField: user => user.Money,
                idField: user => user.Id,
                clock: clock
            );
            IMongoCollection<TestUser> usersCollection = database.GetCollection<TestUser>("users");
            return (bank, usersCollection);
        }

        [Test]
        public async Task performing_transaction_updates_data_and_user_object()
        {
            (IBank<TestUser> bank, IMongoCollection<TestUser> usersCollection) = CreateDbObjects(new MockClock());
            var user = new TestUser { Money = 10 };
            await usersCollection.InsertOneAsync(user);

            var transaction = new Transaction<TestUser>(user, 1, "test");
            TransactionLog log = await bank.PerformTransaction(transaction);

            Assert.AreEqual(10, log.OldBalance);
            Assert.AreEqual(11, log.NewBalance);
            Assert.AreEqual(1, log.Change);
            Assert.AreEqual("test", log.Type);
            TestUser userAfter = await usersCollection.Find(u => u.Id == user.Id).FirstAsync();
            Assert.AreEqual(11, userAfter.Money);
            Assert.AreEqual(11, user.Money); // new balance value was injected into existing object as well
        }

        [Test]
        public async Task fails_transaction_if_user_object_data_is_stale()
        {
            (IBank<TestUser> bank, IMongoCollection<TestUser> usersCollection) = CreateDbObjects(new MockClock());
            var user = new TestUser { Money = 10 };
            await usersCollection.InsertOneAsync(user);
            user.Money = 5; // object is stale, amount does not match database

            var transaction = new Transaction<TestUser>(user, 1, "test");
            InvalidOperationException failure = Assert.ThrowsAsync<InvalidOperationException>(
                () => bank.PerformTransaction(transaction));

            Assert.AreEqual(
                "Tried to perform transaction with stale user data: " +
                $"old balance 5 plus change 1 does not equal new balance 11 for user {user}",
                failure.Message);

            TestUser userAfter = await usersCollection.Find(u => u.Id == user.Id).FirstAsync();
            Assert.AreEqual(10, userAfter.Money);
            Assert.AreEqual(5, user.Money); // no new balance was injected
        }

        [Test]
        public void fails_transaction_if_user_not_found()
        {
            (IBank<TestUser> bank, IMongoCollection<TestUser> _) = CreateDbObjects(new MockClock());
            var user = new TestUser { Money = 10 }; // user not persisted

            var transaction = new Transaction<TestUser>(user, 1, "test");
            UserNotFoundException<TestUser> userNotFound = Assert.ThrowsAsync<UserNotFoundException<TestUser>>(
                () => bank.PerformTransaction(transaction));
            Assert.AreEqual(user, userNotFound.User);
        }

        [Test]
        public async Task performs_multiple_transactions_transactional()
        {
            (IBank<TestUser> bank, IMongoCollection<TestUser> usersCollection) = CreateDbObjects(new MockClock());
            TestUser knownUser = new TestUser { Money = 10 };
            TestUser unknownUser = new TestUser { Money = 20 };
            await usersCollection.InsertOneAsync(knownUser);

            UserNotFoundException<TestUser> userNotFound = Assert.ThrowsAsync<UserNotFoundException<TestUser>>(() =>
                bank.PerformTransactions(new[]
                {
                    new Transaction<TestUser>(knownUser, 3, "test"),
                    new Transaction<TestUser>(unknownUser, -3, "test")
                })
            );

            Assert.AreEqual(unknownUser, userNotFound.User);
            // ensure neither user's balance was modified
            Assert.AreEqual(10, knownUser.Money);
            Assert.AreEqual(20, unknownUser.Money);
            TestUser knownUserAfterwards = await usersCollection.Find(u => u.Id == knownUser.Id).FirstAsync();
            Assert.AreEqual(10, knownUserAfterwards.Money);
        }

        [Test]
        public async Task checks_reserved_money()
        {
            (IBank<TestUser> bank, IMongoCollection<TestUser> usersCollection) = CreateDbObjects(new MockClock());
            TestUser user = new TestUser { Money = 10 };
            TestUser otherUser = new TestUser { Money = 20 };
            await usersCollection.InsertManyAsync(new[] { user, otherUser });
            Task<int> Checker(TestUser u) => Task.FromResult(u == user ? 1 : 0);

            Assert.AreEqual(10, await bank.GetAvailableMoney(user));
            Assert.AreEqual(20, await bank.GetAvailableMoney(otherUser));
            bank.AddReservedMoneyChecker(Checker);
            Assert.AreEqual(9, await bank.GetAvailableMoney(user));
            Assert.AreEqual(20, await bank.GetAvailableMoney(otherUser));
            bank.RemoveReservedMoneyChecker(Checker);
            Assert.AreEqual(10, await bank.GetAvailableMoney(user));
            Assert.AreEqual(20, await bank.GetAvailableMoney(otherUser));
        }

        [Test]
        public void fails_check_money_for_unknown_user()
        {
            (IBank<TestUser> bank, IMongoCollection<TestUser> _) = CreateDbObjects(new MockClock());
            TestUser unknownUser = new TestUser { Money = 0 }; // not persisted
            UserNotFoundException<TestUser> userNotFound = Assert.ThrowsAsync<UserNotFoundException<TestUser>>(
                () => bank.GetAvailableMoney(unknownUser));
            Assert.AreEqual(unknownUser, userNotFound.User);
        }

        [Test]
        public async Task log_has_expected_bson_datatypes()
        {
            MockClock clockMock = new MockClock();
            (IBank<TestUser> bank, IMongoCollection<TestUser> usersCollection) = CreateDbObjects(clockMock);
            TestUser user = new TestUser { Money = 10 };
            await usersCollection.InsertOneAsync(user);
            List<int> list = new List<int> { 1, 2, 3 };
            Dictionary<string, bool> dictionary = new Dictionary<string, bool> { ["yes"] = true, ["no"] = false };
            await bank.PerformTransaction(new Transaction<TestUser>(user, 1, "test",
                new Dictionary<string, object?>
                {
                    ["null_field"] = null,
                    ["int_field"] = 42,
                    ["string_field"] = "foo",
                    ["list_field"] = list,
                    ["dictionary_field"] = dictionary
                }));
            IMongoCollection<BsonDocument> transactionLogCollection =
                usersCollection.Database.GetCollection<BsonDocument>("transactionLog");
            BsonDocument log = await transactionLogCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();

            Assert.IsInstanceOf<BsonObjectId>(log["_id"]);
            Assert.AreEqual(BsonString.Create(user.Id), log["user"]);
            Assert.AreEqual(BsonInt32.Create(1), log["change"]);
            Assert.AreEqual(BsonInt32.Create(10), log["old_balance"]);
            Assert.AreEqual(BsonInt32.Create(11), log["new_balance"]);
            Assert.AreEqual(clockMock.FixedCurrentInstant, log["timestamp"].ToUniversalTime().ToInstant());
            Assert.AreEqual(BsonString.Create("test"), log["type"]);
            Assert.AreEqual(BsonNull.Value, log["null_field"]);
            Assert.AreEqual(BsonInt32.Create(42), log["int_field"]);
            Assert.AreEqual(BsonString.Create("foo"), log["string_field"]);
            Assert.AreEqual(BsonArray.Create(list), log["list_field"]);
            Assert.AreEqual(BsonDocument.Create(dictionary), log["dictionary_field"]);
        }

        [Test]
        public async Task can_handle_null_transaction_type()
        {
            (IBank<TestUser> _, IMongoCollection<TestUser> usersCollection) = CreateDbObjects(new MockClock());
            const string id = "590df61373b975210006fcdf";
            Instant instant = InstantPattern.ExtendedIso.Parse("2017-05-06T16:13:07.314Z").Value;
            IMongoCollection<BsonDocument> bsonTransactionLogCollection =
                usersCollection.Database.GetCollection<BsonDocument>("transactionLog");
            await bsonTransactionLogCollection.InsertOneAsync(BsonDocument.Create(new Dictionary<string, object?>
            {
                ["_id"] = ObjectId.Parse(id),
                ["user"] = "137272735",
                ["change"] = -9,
                ["timestamp"] = instant.ToDateTimeUtc(),
                ["old_balance"] = 25,
                ["new_balance"] = 16,
                ["match"] = 35510,
            }));

            IMongoCollection<TransactionLog> transactionLogCollection =
                usersCollection.Database.GetCollection<TransactionLog>("transactionLog");
            TransactionLog log = await transactionLogCollection.Find(t => t.Id == id).FirstAsync();

            Assert.AreEqual(id, log.Id);
            Assert.AreEqual("137272735", log.UserId);
            Assert.AreEqual(-9, log.Change);
            Assert.AreEqual(25, log.OldBalance);
            Assert.AreEqual(16, log.NewBalance);
            Assert.AreEqual(instant, log.CreatedAt);
            Assert.IsNull(log.Type);
            Assert.AreEqual(new Dictionary<string, object?> { ["match"] = 35510 }, log.AdditionalData);
        }
    }
}
