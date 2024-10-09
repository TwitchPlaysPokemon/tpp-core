using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos
{
    public class Bank<T> : ReserveCheckersBank<T>
    {
        private readonly IMongoCollection<TransactionLog> _transactionLogCollection;
        private readonly IMongoCollection<T> _currencyCollection;
        private readonly IMongoClient _mongoClient;
        private readonly Expression<Func<T, long>> _currencyField;
        private readonly Func<T, long> _currencyFieldAccessor;
        private readonly Expression<Func<T, string>> _idField;
        private readonly Func<T, string> _idFieldAccessor;
        private readonly Action<T, long> _currencyFieldSetter;
        private readonly IClock _clock;

        static Bank()
        {
            BsonClassMap.RegisterClassMap<TransactionLog>(cm =>
            {
                cm.MapIdProperty(t => t.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(ObjectIdAsStringSerializer.Instance);
                cm.MapProperty(t => t.UserId).SetElementName("user");
                cm.MapProperty(t => t.OldBalance).SetElementName("old_balance");
                cm.MapProperty(t => t.NewBalance).SetElementName("new_balance");
                cm.MapProperty(t => t.Change).SetElementName("change");
                cm.MapProperty(t => t.CreatedAt).SetElementName("timestamp");
                cm.MapProperty(t => t.Type).SetElementName("type");
                cm.MapExtraElementsProperty(t => t.AdditionalData);
            });
        }

        public Bank(
            IMongoDatabase database,
            string currencyCollectionName,
            string transactionLogCollectionName,
            Expression<Func<T, long>> currencyField,
            Expression<Func<T, string>> idField,
            IClock clock)
        {
            database.CreateCollectionIfNotExists(transactionLogCollectionName).Wait();
            database.CreateCollectionIfNotExists(currencyCollectionName).Wait();
            _transactionLogCollection = database.GetCollection<TransactionLog>(transactionLogCollectionName);
            _currencyCollection = database.GetCollection<T>(currencyCollectionName);
            _mongoClient = _currencyCollection.Database.Client;
            _currencyField = currencyField;
            _currencyFieldAccessor = _currencyField.Compile();
            _idField = idField;
            _idFieldAccessor = _idField.Compile();
            _clock = clock;

            // create a setter action that lets us modify the balance value after a successful transaction
            ParameterExpression balanceParameter = Expression.Parameter(typeof(long));
            _currencyFieldSetter = Expression.Lambda<Action<T, long>>(
                    Expression.Assign(_currencyField.Body, balanceParameter),
                    _currencyField.Parameters.First(), balanceParameter)
                .Compile();

            InitIndexes();
        }

        private void InitIndexes()
        {
            _transactionLogCollection.Indexes.CreateMany(new[]
            {
                new CreateIndexModel<TransactionLog>(Builders<TransactionLog>.IndexKeys.Ascending(u => u.UserId)),
            });
        }

        private Expression<Func<T, bool>> GetUserIdFilter(string val)
        {
            // basically turn "user => user.Id" into "user => user.Id == transaction.User.Id"
            return Expression.Lambda<Func<T, bool>>(
                Expression.Equal(_idField.Body, Expression.Constant(val)),
                _idField.Parameters.First());
        }

        public override async Task<long> GetTotalMoney(T user)
        {
            string userId = _idFieldAccessor(user);
            FilterDefinition<T> filter = new ExpressionFilterDefinition<T>(GetUserIdFilter(userId));
            T result = await _currencyCollection.Find(filter).FirstOrDefaultAsync()
                       ?? throw new UserNotFoundException<T>(user);
            return _currencyFieldAccessor(result);
        }

        private async Task<TransactionLog> PerformSingleTransaction(
            Transaction<T> transaction,
            IClientSessionHandle session,
            CancellationToken token)
        {
            string userId = _idFieldAccessor(transaction.User);
            FilterDefinition<T> filter = new ExpressionFilterDefinition<T>(GetUserIdFilter(userId));
            UpdateDefinition<T> update = Builders<T>.Update.Inc(_currencyField, transaction.Change);
            var options = new FindOneAndUpdateOptions<T> { IsUpsert = false, ReturnDocument = ReturnDocument.After };
            T entityAfter = await _currencyCollection.FindOneAndUpdateAsync(session, filter, update, options, token)
                            ?? throw new UserNotFoundException<T>(transaction.User);
            long oldBalance = _currencyFieldAccessor(transaction.User);
            long actualNewBalance = _currencyFieldAccessor(entityAfter);
            long expectedNewBalance = oldBalance + transaction.Change;
            if (actualNewBalance < expectedNewBalance)
            {
                // This can happen for multiple concurrent modifications.
                // Since we update the numeric field with an $inc operation, the resulting amount is correct.
                // But to prevent overspending, abort if the actual new balance is _below_ the expected one.
                // An unexpectedly high balance is okay, because that cannot lead to overspending.
                throw new InvalidOperationException(
                    "Tried to perform transaction with stale user data: " +
                    $"old balance {oldBalance} plus change {transaction.Change} " +
                    $"does not equal new balance {actualNewBalance} for user {transaction.User}");
            }
            var transactionLog = new TransactionLog(
                id: string.Empty,
                userId: userId,
                oldBalance: oldBalance,
                newBalance: actualNewBalance,
                change: transaction.Change,
                createdAt: _clock.GetCurrentInstant(),
                type: transaction.Type,
                // don't trust the input not to be modified, make a copy first:
                additionalData: new Dictionary<string, object?>(transaction.AdditionalData)
            );
            await _transactionLogCollection.InsertOneAsync(session, transactionLog, cancellationToken: token);
            return transactionLog;
        }

        public override async Task<IList<TransactionLog>> PerformTransactions(
            IEnumerable<Transaction<T>> transactions,
            CancellationToken token = default)
        {
            List<Action> adjustBalanceActions = new List<Action>();
            using IClientSessionHandle session = await _mongoClient.StartSessionAsync(cancellationToken: token);
            var transactionLogEntries = await session.WithTransactionAsync(async (sessionInner, tokenInner) =>
                {
                    IList<TransactionLog> logEntries = new List<TransactionLog>();
                    foreach (Transaction<T> transaction in transactions)
                    {
                        TransactionLog log = await PerformSingleTransaction(transaction, sessionInner, tokenInner);
                        // defer all in-memory adjustments until the end in case any of the transactions failed.
                        adjustBalanceActions.Add(() => _currencyFieldSetter(transaction.User, log.NewBalance));
                        logEntries.Add(log);
                    }
                    return logEntries;
                },
                new TransactionOptions(), token);
            await session.CommitTransactionAsync(token);
            adjustBalanceActions.ForEach(action => action());
            return transactionLogEntries;
        }
    }
}
