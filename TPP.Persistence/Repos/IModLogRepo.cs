using System.Threading.Tasks;
using NodaTime;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public interface IModLogRepo
    {
        Task<ModLog> LogModAction(User user, string reason, string rule, Instant timestamp);
        Task<long> CountRecentBans(User user, Instant cutoff);
    }
}
