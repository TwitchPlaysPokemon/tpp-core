using System;
using System.Threading.Tasks;
using Models;
using Persistence.Repos;

namespace TppAis
{
    public class DummyUserRepo : IUserRepo
    {
        public Task<User> RecordUser(UserInfo userInfo)
        {
            return Task.FromResult(new User(
                id: userInfo.Id,
                name: userInfo.TwitchDisplayName,
                twitchDisplayName: userInfo.TwitchDisplayName,
                simpleName: userInfo.SimpleName,
                color: userInfo.Color,
                firstActiveAt: DateTime.UnixEpoch,
                lastActiveAt: userInfo.UpdatedAt,
                lastMessageAt: userInfo.UpdatedAt,
                pokeyen: 0,
                tokens: 0
            ));
        }

        public Task<User?> FindBySimpleName(string simpleName)
        {
            throw new NotImplementedException();
        }
    }
}
