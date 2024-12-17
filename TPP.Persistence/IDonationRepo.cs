using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence;

public interface IDonationRepo
{
    public Task<Donation?> FindDonation(int donationId);

    public Task<Donation> InsertDonation(
        int donationId,
        Instant createdAt,
        string userName,
        string? userId,
        int cents,
        string? message);
}
