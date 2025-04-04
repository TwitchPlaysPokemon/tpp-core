using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using TPP.Common;
using TPP.Model;

namespace TPP.Persistence;

public class UserLostBadgeSpeciesEventArgs(string userId, PkmnSpecies species) : EventArgs
{
    public string UserId { get; } = userId;
    public PkmnSpecies Species { get; } = species;
}

/// <summary>
/// Exception thrown when a badge related operation failed because the badge did not exist for a user.
/// </summary>
public class OwnedBadgeNotFoundException(Badge badge) : Exception(
    $"Badge '{badge}' was not found for user {badge.UserId}. " +
    "It's possible the badge object is stale due to a concurrent modification.")
{
    public Badge Badge { get; } = badge;
}

public interface IBadgeStatsRepo
{
    public Task<ImmutableSortedDictionary<PkmnSpecies, BadgeStat>> GetBadgeStats();
    public Task<BadgeStat?> GetBadgeStatsForSpecies(PkmnSpecies species);
}

public interface IBadgeRepo
{
    public Task RenewBadgeStats(IImmutableSet<PkmnSpecies>? onlyTheseSpecies = null);
    public Task<Badge> AddBadge(
        string? userId, PkmnSpecies species, Badge.BadgeSource source, Instant? createdAt = null);
    public Task<IImmutableList<Badge>> FindByUser(string? userId);
    public Task<IImmutableList<Badge>> FindByUserAtTime(string userId, Instant timestamp);
    public Task<IImmutableList<string>> FindOwnerIdsForSpecies(PkmnSpecies species);
    public Task<IImmutableList<Badge>> FindByUserAndSpecies(string? userId, PkmnSpecies species, int? limit = null);
    public Task<long> CountByUserAndSpecies(string? userId, PkmnSpecies species);
    public Task<ImmutableSortedDictionary<PkmnSpecies, int>> CountByUserPerSpecies(string? userId);
    public Task<bool> HasUserBadge(string? userId, PkmnSpecies species);

    public event EventHandler<UserLostBadgeSpeciesEventArgs> UserLostBadgeSpecies;

    public Task<IImmutableList<Badge>> TransferBadges(
        IImmutableList<Badge> badges, string? recipientUserId, string reason,
        IDictionary<string, object?> additionalData);

    Task WatchAndHandleBadgeStatUpdates(CancellationToken cancellationToken);
}
