using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Attributes;
using NodaTime;
using TPP.Persistence.Repos;

namespace TPP.Core
{
    internal class UpdateToken
    {
        public const string KeyValueId = "database_exclusivity_token";
        [BsonId]
        public string Id { get; init; } = KeyValueId;
        [BsonElement("updated_at")]
        public Instant UpdatedAt { get; init; }
    }

    internal sealed class ProxyAsyncDisposable : IAsyncDisposable
    {
        private readonly Func<ValueTask> _dispose;
        public ProxyAsyncDisposable(Func<ValueTask> dispose) => _dispose = dispose;
        public ValueTask DisposeAsync() => _dispose();
    }

    public sealed class DatabaseExclusivity
    {
        private static readonly Duration TimeoutDuration = Duration.FromSeconds(10);
        private static readonly Duration RefreshInterval = Duration.FromSeconds(2);

        private readonly ILogger<DatabaseExclusivity> _logger;
        private readonly IClock _clock;
        private readonly IKeyValueStore _keyValueStore;

        public DatabaseExclusivity(
            ILogger<DatabaseExclusivity> logger,
            IClock clock,
            IKeyValueStore keyValueStore)
        {
            _logger = logger;
            _clock = clock;
            _keyValueStore = keyValueStore;
        }

        public async Task<IAsyncDisposable> Acquire()
        {
            await AcquireToken();
            CancellationTokenSource cancellationTokenSource = new();
            Task refreshWorker = Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(RefreshInterval.ToTimeSpan(), cancellationTokenSource.Token);
                    await SetRefreshToken();
                }
            }, cancellationTokenSource.Token);
            return new ProxyAsyncDisposable(async () =>
            {
                cancellationTokenSource.Cancel();
                try { await refreshWorker; }
                catch (OperationCanceledException) { }
                await ReleaseToken();
            });
        }

        private async Task AcquireToken()
        {
            while (true)
            {
                UpdateToken? updateToken = await _keyValueStore.Get<UpdateToken>(UpdateToken.KeyValueId);
                Instant now = _clock.GetCurrentInstant();
                if (updateToken == null || updateToken.UpdatedAt + TimeoutDuration <= now)
                {
                    await SetRefreshToken();
                    return;
                }
                Duration expiresIn = updateToken.UpdatedAt + TimeoutDuration - now;
                _logger.LogWarning("Database exclusivity token is still being held! " +
                                   "Only once instance of dualcore mode may run at a time. " +
                                   "Trying again in {Seconds:#.#} seconds", expiresIn.TotalSeconds);
                await Task.Delay(expiresIn.ToTimeSpan());
            }
        }

        private async Task SetRefreshToken() =>
            await _keyValueStore.Set(UpdateToken.KeyValueId,
                new UpdateToken { UpdatedAt = _clock.GetCurrentInstant() });

        private async Task ReleaseToken() =>
            await _keyValueStore.Delete<UpdateToken>(UpdateToken.KeyValueId);
    }
}
