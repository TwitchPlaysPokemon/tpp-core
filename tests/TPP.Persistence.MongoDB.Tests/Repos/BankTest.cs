using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;
using NUnit.Framework;
using TPP.Model;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos;

internal class TestUser
{
    [BsonId] public string Id { get; } = Guid.NewGuid().ToString();
    [BsonElement] public long Money { get; set; }
    public override string ToString() => Id;
}

internal class MockClock : IClock
{
    public Instant FixedCurrentInstant = Instant.FromUnixTimeSeconds(1234567890);
    public Instant GetCurrentInstant() => FixedCurrentInstant;
}

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

        Assert.That(log.OldBalance, Is.EqualTo(10));
        Assert.That(log.NewBalance, Is.EqualTo(11));
        Assert.That(log.Change, Is.EqualTo(1));
        Assert.That(log.Type, Is.EqualTo("test"));
        TestUser userAfter = await usersCollection.Find(u => u.Id == user.Id).FirstAsync();
        Assert.That(userAfter.Money, Is.EqualTo(11));
        Assert.That(user.Money, Is.EqualTo(11)); // new balance value was injected into existing object as well
    }

    [Test]
    public async Task fails_transaction_if_user_object_data_is_stale_and_higher()
    {
        (IBank<TestUser> bank, IMongoCollection<TestUser> usersCollection) = CreateDbObjects(new MockClock());
        var user = new TestUser { Money = 10 };
        await usersCollection.InsertOneAsync(user);
        user.Money = 15; // amount in the database is unexpectedly lower than this

        var transaction = new Transaction<TestUser>(user, 1, "test");
        InvalidOperationException failure = Assert.ThrowsAsync<InvalidOperationException>(
            () => bank.PerformTransaction(transaction))!;

        Assert.That(
            failure.Message, Is.EqualTo("Tried to perform transaction with stale user data: " +
                                        $"old balance 15 plus change 1 does not equal new balance 11 for user {user}"));

        TestUser userAfter = await usersCollection.Find(u => u.Id == user.Id).FirstAsync();
        Assert.That(userAfter.Money, Is.EqualTo(10));
        Assert.That(user.Money, Is.EqualTo(15)); // no new balance was injected
    }

    [Test]
    public void fails_transaction_if_user_not_found()
    {
        (IBank<TestUser> bank, IMongoCollection<TestUser> _) = CreateDbObjects(new MockClock());
        var user = new TestUser { Money = 10 }; // user not persisted

        var transaction = new Transaction<TestUser>(user, 1, "test");
        UserNotFoundException<TestUser> userNotFound = Assert.ThrowsAsync<UserNotFoundException<TestUser>>(
            () => bank.PerformTransaction(transaction))!;
        Assert.That(userNotFound.User, Is.EqualTo(user));
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
        )!;

        Assert.That(userNotFound.User, Is.EqualTo(unknownUser));
        // ensure neither user's balance was modified
        Assert.That(knownUser.Money, Is.EqualTo(10));
        Assert.That(unknownUser.Money, Is.EqualTo(20));
        TestUser knownUserAfterwards = await usersCollection.Find(u => u.Id == knownUser.Id).FirstAsync();
        Assert.That(knownUserAfterwards.Money, Is.EqualTo(10));
    }

    [Test]
    public async Task checks_reserved_money()
    {
        (IBank<TestUser> bank, IMongoCollection<TestUser> usersCollection) = CreateDbObjects(new MockClock());
        TestUser user = new TestUser { Money = 10 };
        TestUser otherUser = new TestUser { Money = 20 };
        await usersCollection.InsertManyAsync(new[] { user, otherUser });
        Task<long> Checker(TestUser u) => Task.FromResult(u == user ? 1L : 0L);

        Assert.That(await bank.GetAvailableMoney(user), Is.EqualTo(10));
        Assert.That(await bank.GetAvailableMoney(otherUser), Is.EqualTo(20));
        bank.AddReservedMoneyChecker(Checker);
        Assert.That(await bank.GetAvailableMoney(user), Is.EqualTo(9));
        Assert.That(await bank.GetAvailableMoney(otherUser), Is.EqualTo(20));
        bank.RemoveReservedMoneyChecker(Checker);
        Assert.That(await bank.GetAvailableMoney(user), Is.EqualTo(10));
        Assert.That(await bank.GetAvailableMoney(otherUser), Is.EqualTo(20));
    }

    [Test]
    public void fails_check_money_for_unknown_user()
    {
        (IBank<TestUser> bank, IMongoCollection<TestUser> _) = CreateDbObjects(new MockClock());
        TestUser unknownUser = new TestUser { Money = 0 }; // not persisted
        UserNotFoundException<TestUser> userNotFound = Assert.ThrowsAsync<UserNotFoundException<TestUser>>(
            () => bank.GetAvailableMoney(unknownUser))!;
        Assert.That(userNotFound.User, Is.EqualTo(unknownUser));
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
        Assert.That(log["user"], Is.EqualTo(BsonString.Create(user.Id)));
        Assert.That(log["change"], Is.EqualTo(BsonInt64.Create(1)));
        Assert.That(log["old_balance"], Is.EqualTo(BsonInt64.Create(10)));
        Assert.That(log["new_balance"], Is.EqualTo(BsonInt64.Create(11)));
        Assert.That(log["timestamp"].ToUniversalTime().ToInstant(), Is.EqualTo(clockMock.FixedCurrentInstant));
        Assert.That(log["type"], Is.EqualTo(BsonString.Create("test")));
        Assert.That(log["null_field"], Is.EqualTo(BsonNull.Value));
        Assert.That(log["int_field"], Is.EqualTo(BsonInt32.Create(42)));
        Assert.That(log["string_field"], Is.EqualTo(BsonString.Create("foo")));
        Assert.That(log["list_field"], Is.EqualTo(BsonArray.Create(list)));
        Assert.That(log["dictionary_field"], Is.EqualTo(BsonDocument.Create(dictionary)));
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

        Assert.That(log.Id, Is.EqualTo(id));
        Assert.That(log.UserId, Is.EqualTo("137272735"));
        Assert.That(log.Change, Is.EqualTo(-9));
        Assert.That(log.OldBalance, Is.EqualTo(25));
        Assert.That(log.NewBalance, Is.EqualTo(16));
        Assert.That(log.CreatedAt, Is.EqualTo(instant));
        Assert.IsNull(log.Type);
        Assert.That(log.AdditionalData, Is.EqualTo(new Dictionary<string, object?> { ["match"] = 35510 }));
    }

    [Test]
    public async Task can_handle_concurrent_transactions()
    {
        (IBank<TestUser> bank, IMongoCollection<TestUser> usersCollection) = CreateDbObjects(new MockClock());
        var user = new TestUser { Money = 10 };
        await usersCollection.InsertOneAsync(user);
        const int numTransactions = 100;

        TransactionLog[] transactionLogs = await Task.WhenAll(Enumerable.Range(0, numTransactions)
            .Select(i => bank.PerformTransaction(new Transaction<TestUser>(user, 1, "test-" + i))));

        Assert.That(transactionLogs.Length, Is.EqualTo(numTransactions));
        TestUser userAfterwards = await usersCollection.Find(u => u.Id == user.Id).FirstAsync();
        Assert.That(userAfterwards.Money, Is.EqualTo(10 + numTransactions));
    }
}
