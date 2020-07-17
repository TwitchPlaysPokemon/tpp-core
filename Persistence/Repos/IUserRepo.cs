using System.Threading.Tasks;
using Common;
using Persistence.Models;

namespace Persistence.Repos
{
    public interface IUserRepo
    {
        public Task<User> RecordUser(UserInfo userInfo);
        public Task<User?> FindBySimpleName(string simpleName);

        public Task<User> SetSelectedBadge(User user, PkmnSpecies? badge);
    }
}
