using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence;

public static class BadgeLogType
{
    public const string TransferGift = "gift";
    public const string TransferGiftRemote = "gift_remote";
    public const string Transmutation = "transmutation";
    public const string TransmutationRollback = "transmutation_rollback";
    public const string ManualTransfer = "manual_transfer";
    public const string ManualRemoval = "manual_removal";
    public const string Breaking = "breaking";
    public const string TransferAssets = "transferassets";
    public const string Liquidation = "liquidation";
    public const string Purchase = "purchase";

    // collect all the types being used here instead of scattering string literals across the codebase
}

public interface IBadgeLogRepo
{
    public Task<BadgeLog> Log(
        string badgeId,
        string badgeLogType,
        string? userId,
        string? oldUserId,
        Instant timestamp,
        IDictionary<string, object?>? additionalData = null);

    /// Searches the badge logs to find IDs of badges that a user had at a given point in time. Note that because the
    /// badge log does not know when a badge was created, this _may_ include badge IDs of badges that got created after
    /// the given timestamp, so filtering on 'badge.CreatedAt &lt;= timestamp' still needs to be performed afterward.
    public Task<IImmutableSet<string>> FindBadgeIdsByUserAtTime(string userId, Instant timestamp);
}
