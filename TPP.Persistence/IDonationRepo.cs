using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence;

public interface IDonationRepo
{
    public Task<Donation?> FindDonation(long donationId);

    public Task<Donation?> GetMostRecentDonation();
    public Task<Donation> InsertDonation(
        long donationId,
        Instant createdAt,
        string userName,
        string? userId,
        int cents,
        string? message);

    /// Calculates the total cents donated per user,
    /// optionally restricted to a given set of users
    /// and/or a minimal total amount of cents to include in the result.
    /// Donations without a user ID (username wasn't known at donation time) are ignored.
    public Task<IImmutableDictionary<string, long>> GetCentsPerUser(
        int minTotalCents = 0,
        ISet<string>? userIdFilter = null);

    public Task<SortedDictionary<DonationRecordBreakType, Donation>> GetRecordDonations(Instant now);
}
