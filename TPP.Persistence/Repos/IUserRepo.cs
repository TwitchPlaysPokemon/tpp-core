using System.Threading.Tasks;
using NodaTime;
using TPP.Common;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public interface IUserRepo
    {
        public Task<User> RecordUser(UserInfo userInfo);
        public Task<User?> FindBySimpleName(string simpleName);
        public Task<User?> FindByDisplayName(string displayName);
        public Task<User> SetSelectedBadge(User user, PkmnSpecies? badge);
        public Task<User> SetSelectedEmblem(User user, int? emblem);
        public Task<User> SetGlowColor(User user, string? glowColor);
        public Task<User> SetGlowColorUnlocked(User user, bool unlocked);
        public Task<User> SetDisplayName(User user, string displayName);

        public Task<User> SetIsSubscribed(User user, bool isSubscribed);
        public Task<User> SetSubscriptionInfo(
            User user, int monthsSubscribed, SubscriptionTier tier, int loyaltyLeague, Instant? subscriptionUpdatedAt);

        /// Unselects the specified species as the presented badge if it is the currently equipped species.
        /// Used for resetting the equipped badge after a user lost all of that species' badges.
        /// Returns true if the badge was unequipped, otherwise false.
        public Task<bool> UnselectBadgeIfSpeciesSelected(string userId, PkmnSpecies species);
    }
}
