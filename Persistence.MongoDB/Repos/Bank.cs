using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using Persistence.Models;
using Persistence.MongoDB.Serializers;
using Persistence.Repos;

namespace Persistence.MongoDB.Repos
{
    public class Bank<T> : ReserveCheckersBank<T>
    {
        private readonly IMongoCollection<TransactionLog> _transactionLogCollection;
        private readonly IMongoCollection<T> _currencyCollection;
        private readonly IMongoClient _mongoClient;
        private readonly Expression<Func<T, int>> _currencyField;
        private readonly Func<T, int> _currencyFieldAccessor;
        private readonly Expression<Func<T, string>> _idField;
        private readonly Func<T, string> _idFieldAccessor;
        private readonly Action<T, int> _currencyFieldSetter;
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
            Expression<Func<T, int>> currencyField,
            Expression<Func<T, string>> idField,
            IClock clock)
        {
            database.CreateCollection(transactionLogCollectionName);
            database.CreateCollection(currencyCollectionName);
            _transactionLogCollection = database.GetCollection<TransactionLog>(transactionLogCollectionName);
            _currencyCollection = database.GetCollection<T>(currencyCollectionName);
            _mongoClient = _currencyCollection.Database.Client;
            _currencyField = currencyField;
            _currencyFieldAccessor = _currencyField.Compile();
            _idField = idField;
            _idFieldAccessor = _idField.Compile();
            _clock = clock;

            // create a setter action that lets us modify the balance value after a successful transaction
            var balanceParameter = Expression.Parameter(typeof(int));
            _currencyFieldSetter = Expression.Lambda<Action<T, int>>(
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

        public override async Task<int> GetTotalMoney(T user)
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
            int oldBalance = _currencyFieldAccessor(transaction.User);
            int newBalance = _currencyFieldAccessor(entityAfter);
            if (oldBalance + transaction.Change != newBalance)
            {
                throw new InvalidOperationException("tried to perform transaction with stale user data");
            }
            var transactionLog = new TransactionLog(
                id: string.Empty,
                userId: userId,
                oldBalance: oldBalance,
                newBalance: newBalance,
                change: transaction.Change,
                createdAt: _clock.GetCurrentInstant(),
                type: transaction.Type,
                additionalData: transaction.AdditionalData
            );
            await _transactionLogCollection.InsertOneAsync(session, transactionLog, cancellationToken: token);
            Debug.Assert(transactionLog.Id.Length > 0, "The MongoDB driver injected a generated ID");
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
