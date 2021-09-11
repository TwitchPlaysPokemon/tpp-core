using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NodaTime;
using TPP.Common;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public class UserLostBadgeSpeciesEventArgs : EventArgs
    {
        public string UserId { get; }
        public PkmnSpecies Species { get; }

        public UserLostBadgeSpeciesEventArgs(string userId, PkmnSpecies species)
        {
            UserId = userId;
            Species = species;
        }
    }

    /// <summary>
    /// Exception thrown when a badge related operation failed because the badge did not exist for a user.
    /// </summary>
    public class OwnedBadgeNotFoundException : Exception
    {
        public Badge Badge { get; }

        public OwnedBadgeNotFoundException(Badge badge) :
            base($"Badge '{badge}' was not found for user {badge.UserId}. " +
                 "It's possible the badge object is stale due to a concurrent modification.")
        {
            Badge = badge;
        }
    }

    public interface IBadgeRepo
    {
        public Task<Badge> AddBadge(
            string? userId, PkmnSpecies species, Badge.BadgeSource source, string? form, bool shiny, Instant? createdAt = null);
        public Task<List<Badge>> FindAllByUser(string? userId);
        public Task<List<Badge>> FindByUserAndSpecies(string? userId, PkmnSpecies species);
        public Task<List<Badge>> FindAllByCustom(string? userId, PkmnSpecies? species, string? form, Badge.BadgeSource? source, bool? shiny);
        public Task<long> CountByUserAndSpecies(string? userId, PkmnSpecies species);
        public Task<ImmutableSortedDictionary<PkmnSpecies, int>> CountByUserPerSpecies(string? userId);
        public Task<bool> HasUserBadge(string? userId, PkmnSpecies species);

        public event EventHandler<UserLostBadgeSpeciesEventArgs> UserLostBadgeSpecies;

        public Task<IImmutableList<Badge>> TransferBadges(
            IImmutableList<Badge> badges, string? recipientUserId, string reason,
            IDictionary<string, object?> additionalData);

        public Task<Badge> SetBadgeSellPrice(Badge badge, int price);
        public Task<List<Badge>> FindAllForSaleByCustom(string? userId, PkmnSpecies? species, string? form, Badge.BadgeSource? source, bool? shiny);
    }
}
