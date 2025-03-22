using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TPP.Common;
using TPP.Persistence;

namespace TPP.Core;

public sealed class BadgeStatsRefreshWorker(IBadgeRepo badgeRepo, IBadgeStatsRepo badgeStatsRepo) : IWithLifecycle
{
    public async Task Start(CancellationToken cancellationToken)
    {
        // start update task before initial update, so we don't miss anything in-between
        Task watchBadgeUpdatesTask = badgeRepo.WatchBadgeUpdates(cancellationToken, async updates =>
        {
            ImmutableHashSet<PkmnSpecies> dirtySpecies = updates
                .Where(update => update switch
                {
                    { Before: null } => update.After?.UserId is not null,
                    { After: null } => update.Before?.UserId is not null,
                    { Before: var before, After: var after }
                        => (before.UserId is null) != (after.UserId is null) || // consumed or consumption undone
                           (before.UserId is not null && before.Species != after.Species), // changed species
                })
                .SelectMany(update => update switch
                {
                    { Before.Species: var sp1, After.Species: var sp2 } => [sp1, sp2],
                    { Before.Species: var sp } => [sp],
                    { After.Species: var sp } => [sp],
                    _ => Array.Empty<PkmnSpecies>()
                })
                .ToImmutableHashSet();

            if (!dirtySpecies.IsEmpty)
                await badgeStatsRepo.RenewBadgeStats(dirtySpecies);
        });

        // initial refresh of all stats at boot
        await badgeStatsRepo.RenewBadgeStats();

        await watchBadgeUpdatesTask;
        if (!cancellationToken.IsCancellationRequested)
            throw new Exception("BadgeStatsRefreshWorker unexpectedly terminated without cancellation");
    }
}
