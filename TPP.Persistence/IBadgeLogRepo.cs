using System.Collections.Generic;
using System.Threading.Tasks;
using NodaTime;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public static class BadgeLogType
    {
        public const string TransferGift = "gift";
        public const string TransferGiftRemote = "gift_remote";

        // collect all the types being used here instead of scattering string literals across the codebase
    }

    public interface IBadgeLogRepo
    {
        public Task<BadgeLog> Log(
            string badgeId,
            string badgeLogType,
            string? userId,
            Instant timestamp,
            IDictionary<string, object?>? additionalData = null);
    }
}
