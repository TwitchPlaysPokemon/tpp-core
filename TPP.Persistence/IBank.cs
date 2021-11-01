using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TPP.Common;
using TPP.Model;

namespace TPP.Persistence
{
    /// <summary>
    /// Exception thrown when a user related operation failed because the user did not exist.
    /// </summary>
    public class UserNotFoundException<T> : Exception
    {
        public T User { get; }

        public UserNotFoundException(T user) : base($"User '{user}' was not found")
        {
            User = user;
        }
    }

    public static class TransactionType
    {
        public const string SecondaryColorUnlock = "secondary_color_unlock";
        public const string ManualAdjustment = "manual_adjustment";
        public const string DonationGive = "donation_give";
        public const string DonationReceive = "donation_recieve"; // typo kept for backwards-compatibility for now
        public const string Match = "match";
        public const string Subscription = "subscription";
        public const string SubscriptionGift = "subscription gift";
        public const string Welfare = "welfare";
        public const string Transmutation = "transmutation";

        // collect all the types being used here instead of scattering string literals across the codebase
    }

    /// <summary>
    /// A single atomic monetary transaction that can be performed.
    /// </summary>
    /// <typeparam name="T">User object type the transaction is performed on, typically <see cref="User"/></typeparam>
    public sealed record Transaction<T>
    {
        /// <summary>
        /// User object the transaction will be performed on.
        /// </summary>
        public T User { get; }
        /// <summary>
        /// The monetary amount delta that should be applied, may also be negative for deductions.
        /// </summary>
        public long Change { get; }
        /// <summary>
        /// What type of transaction this is.
        /// </summary>
        public string Type { get; }
        /// <summary>
        /// Any additional data that should be stored with the transaction.
        /// </summary>
        public IDictionary<string, object?> AdditionalData { get; }

        public Transaction(
            T user,
            long change,
            string type,
            IDictionary<string, object?>? additionalData = null)
        {
            User = user;
            Change = change;
            Type = type;
            AdditionalData = additionalData ?? ImmutableDictionary<string, object?>.Empty;
        }

        public override string ToString()
            => $"Transaction({User}{Change:+#;-#} of type {Type} " +
               $"with data {string.Join(",", AdditionalData.Select(kv => kv.Key + "=" + kv.Value))})";

        public bool Equals(Transaction<T>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(User, other.User)
                   && Change == other.Change
                   && Type == other.Type
                   && AdditionalData.DictionaryEqual(other.AdditionalData); // <-- Equals() is overridden just for this
        }

        public override int GetHashCode() => HashCode.Combine(User, Change, Type, AdditionalData);
    }

    /// <summary>
    /// A bank is capable of safely handling some entity's numeric fields that represent a monetary value.
    /// It offers functionality for safe transactional adjustments, money reserving, and database logging.
    /// </summary>
    /// <typeparam name="T">User object type this bank operates on, typically <see cref="User"/></typeparam>
    public interface IBank<T>
    {
        /// <summary>
        /// Delegate retrieving reserved money for a specific user.
        /// If the delegate does not find the supplied user, 0 should be returned.
        /// Reserved money is money that is not yet spent by the user, but may have to be
        /// spent in the future and hence must stay available to prevent overspending.
        /// This may apply to e.g. ongoing pinball games or item buy offers.
        /// </summary>
        /// <param name="user">User to retrieve reserved money for.</param>
        delegate Task<long> ReservedMoneyChecker(T user);

        /// <summary>
        /// Add a delegate that retrieves reserved money for a user.
        /// </summary>
        /// <param name="checker">The delegate retrieving the amount of reserved money.</param>
        void AddReservedMoneyChecker(ReservedMoneyChecker checker);

        /// <summary>
        /// Removes a delegate retrieving reserved money that was previously
        /// added with <see cref="AddReservedMoneyChecker"/>.
        /// </summary>
        /// <param name="checker">The delegate retrieving the amount of reserved money.</param>
        void RemoveReservedMoneyChecker(ReservedMoneyChecker checker);

        /// <summary>
        /// Gets the amount of currently reserved money for a user.
        /// See <see cref="ReservedMoneyChecker"/> for an explanation on what reserved money is.
        /// </summary>
        /// <param name="user">User to retrieve reserved money for.</param>
        /// <returns>The amount of reserved money.</returns>
        Task<long> GetReservedMoney(T user);

        /// <summary>
        /// Gets the total amount of money a user has.
        /// This likely is _not_ what you are looking for, since it does not incorporate reserved money.
        /// Using this method for currency checks may lead to overspending.
        /// <seealso cref="GetAvailableMoney"/>
        /// </summary>
        /// <param name="user">User to retrieve total money for.</param>
        /// <returns>The amount of total money.</returns>
        /// <exception cref="UserNotFoundException{T}">Thrown if the user does not exist.</exception>
        Task<long> GetTotalMoney(T user);

        /// <summary>
        /// Gets the amount of currently available money a user has.
        /// This is likely what you are looking for, since it does incorporate reserved money.
        /// The amount of money returned by this method may be spent without risk of overspending.
        /// <seealso cref="GetTotalMoney"/>
        /// </summary>
        /// <param name="user">User to retrieve available money for.</param>
        /// <returns>The amount of available money.</returns>
        /// <exception cref="UserNotFoundException{T}">Thrown if the user does not exist.</exception>
        Task<long> GetAvailableMoney(T user);

        /// <summary>
        /// Perform multiple monetary transactions atomically.
        /// Either all transactions succeed, or none do.
        /// </summary>
        /// <param name="transactions">enumerable of transactions to perform</param>
        /// <param name="token">cancellation token</param>
        /// <returns>Created transaction log entries.
        /// The returned list will have the same size and order as the supplied transactions.</returns>
        /// <exception cref="UserNotFoundException{T}">Thrown if a user does not exist.</exception>
        Task<IList<TransactionLog>> PerformTransactions(
            IEnumerable<Transaction<T>> transactions,
            CancellationToken token = default);

        /// <summary>
        /// Perform a single monetary transaction atomically.
        /// Either the transaction succeeds in its entirety, or none of it does.
        /// </summary>
        /// <param name="transaction">transaction to perform</param>
        /// <param name="token">cancellation token</param>
        /// <returns>Created transaction log entry.</returns>
        /// <exception cref="UserNotFoundException{T}">Thrown if the user does not exist.</exception>
        public Task<TransactionLog> PerformTransaction(
            Transaction<T> transaction,
            CancellationToken token = default);
    }

    /// <summary>
    /// Abstract bank implementing money reserving using externally passed-in reserved money checker functions.
    /// </summary>
    /// <typeparam name="T">User object type this bank operates on, typically <see cref="User"/></typeparam>
    public abstract class ReserveCheckersBank<T> : IBank<T>
    {
        private readonly IList<IBank<T>.ReservedMoneyChecker> _checkers = new List<IBank<T>.ReservedMoneyChecker>();

        public void AddReservedMoneyChecker(IBank<T>.ReservedMoneyChecker checker) => _checkers.Add(checker);
        public void RemoveReservedMoneyChecker(IBank<T>.ReservedMoneyChecker checker) => _checkers.Remove(checker);

        public async Task<long> GetReservedMoney(T user)
        {
            IEnumerable<Task<long>> amounts = _checkers.Select(async checker => await checker(user));
            return (await Task.WhenAll(amounts)).Sum();
        }

        public abstract Task<long> GetTotalMoney(T user);

        public async Task<long> GetAvailableMoney(T user) =>
            await GetTotalMoney(user) - await GetReservedMoney(user);

        public abstract Task<IList<TransactionLog>> PerformTransactions(
            IEnumerable<Transaction<T>> transactions,
            CancellationToken token = default);

        public async Task<TransactionLog> PerformTransaction(
            Transaction<T> transaction,
            CancellationToken token = default) =>
            (await PerformTransactions(new[] { transaction }, token)).First();
    }
}
