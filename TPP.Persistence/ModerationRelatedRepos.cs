using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence
{
    public interface IModLogRepo
    {
        Task<ModLog> LogModAction(User user, string reason, string rule, Instant timestamp);
        Task<long> CountRecentBans(User user, Instant cutoff);
    }
    public interface IBanLogRepo
    {
        Task<BanLog> LogBan(string userId, string type, string reason, string issuerUserId, Instant timestamp);
        Task<BanLog?> FindMostRecent(string userId);
    }
    public interface ITimeoutLogRepo
    {
        Task<TimeoutLog> LogTimeout(
            string userId, string type, string reason, string issuerUserId, Instant timestamp, Duration? duration);
    }
}
