using System.Threading.Tasks;
using Persistence.Models;

namespace Persistence.Repos
{
    public interface IUserRepo
    {
        public Task<User> RecordUser(UserInfo userInfo);
    }
}
