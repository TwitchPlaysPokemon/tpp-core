using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence;

public interface IModLogRepo
{
    Task<ModLog> LogModAction(User user, string reason, string rule, Instant timestamp);
    Task<long> CountRecentBans(User user, Instant cutoff);
}
