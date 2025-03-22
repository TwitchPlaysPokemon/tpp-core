using System;
using System.Threading;
using System.Threading.Tasks;
using TPP.Persistence;

namespace TPP.Core;

public sealed class BadgeStatsRefreshWorker(IBadgeRepo badgeRepo) : IWithLifecycle
{
    public async Task Start(CancellationToken cancellationToken)
    {
        // Start the task before the full refresh, so we don't miss anything in-between
        var watchTask = badgeRepo.WatchAndHandleBadgeStatUpdates(cancellationToken);

        // Full stats refresh at every boot
        await badgeRepo.RenewBadgeStats();

        await watchTask;

        if (!cancellationToken.IsCancellationRequested)
            throw new Exception("BadgeStatsRefreshWorker unexpectedly ended without cancellation");
    }
}
