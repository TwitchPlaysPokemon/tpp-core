using System.Threading.Tasks;
using TPP.Common;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public interface IUserRepo
    {
        public Task<User> RecordUser(UserInfo userInfo);
        public Task<User?> FindBySimpleName(string simpleName);

        public Task<User> SetSelectedBadge(User user, PkmnSpecies? badge);
        public Task<User> SetSelectedEmblem(User user, int? emblem);
        public Task<User> SetGlowColor(User user, string? glowColor);
        public Task<User> SetGlowColorUnlocked(User user, bool unlocked);
        public Task<User> SetDisplayName(User user, string displayName);
    }
}
