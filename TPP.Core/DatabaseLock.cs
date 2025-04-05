using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Attributes;
using NodaTime;
using TPP.Persistence;

namespace TPP.Core;

internal class DatabaseLockEntry
{
    public const string KeyValueId = "database_lock";
    [BsonId]
    public string Id { get; init; } = KeyValueId;
    [BsonElement("refreshed_at")]
    public Instant RefreshedAt { get; init; }
}

internal sealed class ProxyAsyncDisposable(Func<ValueTask> dispose) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => dispose();
}

public sealed class DatabaseLock(
    ILogger<DatabaseLock> logger,
    IClock clock,
    IKeyValueStore keyValueStore)
{
    private static readonly Duration TimeoutDuration = Duration.FromSeconds(10);
    private static readonly Duration RefreshInterval = Duration.FromSeconds(2);

    public async Task<IAsyncDisposable> Acquire()
    {
        await AcquireLockInDatabase();
        CancellationTokenSource cancellationTokenSource = new();
        Task refreshWorker = Task.Run(async () =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(RefreshInterval.ToTimeSpan(), cancellationTokenSource.Token);
                await SetRefreshTokenInDatabase();
            }
        }, cancellationTokenSource.Token);
        return new ProxyAsyncDisposable(async () =>
        {
            cancellationTokenSource.Cancel();
            try { await refreshWorker; }
            catch (OperationCanceledException) { }
            await ReleaseLockInDatabase();
        });
    }

    private async Task AcquireLockInDatabase()
    {
        while (true)
        {
            DatabaseLockEntry? updateToken = await keyValueStore.Get<DatabaseLockEntry>(DatabaseLockEntry.KeyValueId);
            Instant now = clock.GetCurrentInstant();
            if (updateToken == null || updateToken.RefreshedAt + TimeoutDuration <= now)
            {
                await SetRefreshTokenInDatabase();
                return;
            }
            Duration expiresIn = updateToken.RefreshedAt + TimeoutDuration - now;
            logger.LogWarning("Database lock is still being held! " +
                               "Only once instance of dualcore mode may run at a time. " +
                               "Trying again in {Seconds:#.#} seconds", expiresIn.TotalSeconds);
            await Task.Delay(expiresIn.ToTimeSpan());
        }
    }

    private async Task SetRefreshTokenInDatabase() =>
        await keyValueStore.Set(DatabaseLockEntry.KeyValueId,
            new DatabaseLockEntry { RefreshedAt = clock.GetCurrentInstant() });

    private async Task ReleaseLockInDatabase() =>
        await keyValueStore.Delete<DatabaseLockEntry>(DatabaseLockEntry.KeyValueId);
}
