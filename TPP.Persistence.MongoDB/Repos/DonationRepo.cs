using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence.MongoDB.Repos;

public class DonationRepo(IMongoDatabase database) : IDonationRepo, IAsyncInitRepo
{
    private const string CollectionName = "donations";

    static DonationRepo()
    {
        BsonClassMap.RegisterClassMap<Donation>(cm =>
        {
            cm.MapIdProperty(b => b.DonationId);
            cm.MapProperty(b => b.CreatedAt).SetElementName("created_at");
            cm.MapProperty(b => b.UserName).SetElementName("name");
            cm.MapProperty(b => b.UserId).SetElementName("user");
            cm.MapProperty(b => b.Message).SetElementName("message");
            cm.MapProperty(b => b.Cents).SetElementName("cents");
        });
    }

    public readonly IMongoCollection<Donation> Collection = database.GetCollection<Donation>(CollectionName);

    public async Task InitializeAsync()
    {
        await Collection.Indexes.CreateManyAsync([
            new CreateIndexModel<Donation>(Builders<Donation>.IndexKeys.Ascending(u => u.CreatedAt)),
            new CreateIndexModel<Donation>(Builders<Donation>.IndexKeys.Ascending(u => u.UserId))
        ]);
    }

    public async Task<Donation?> FindDonation(long donationId) =>
        await Collection.Find(p => p.DonationId == donationId).FirstOrDefaultAsync();

    public async Task<Donation?> GetMostRecentDonation() =>
        await Collection.AsQueryable().OrderByDescending(u => u.CreatedAt).FirstOrDefaultAsync();

    public async Task<Donation> InsertDonation(
        long donationId, Instant createdAt, string userName, string? userId, int cents, string? message = null)
    {
        Donation donation = new(
            donationId: donationId,
            createdAt: createdAt,
            userName: userName,
            userId: userId,
            cents: cents,
            message: message);

        await Collection.InsertOneAsync(donation);

        return donation;
    }

    public async Task<IImmutableDictionary<string, long>> GetCentsPerUser(
        int minTotalCents = 0,
        ISet<string>? userIdFilter = null)
    {
        IQueryable<Donation> query = Collection.AsQueryable()
            .Where(donation => donation.UserId != null);
        if (userIdFilter is not null)
            query = query.Where(donation => userIdFilter.Contains(donation.UserId!));
        var result = await query.GroupBy(donation => donation.UserId!)
            .Select(grp => new
            {
                UserId = grp.Key,
                Cents = grp.Sum(c => (long)c.Cents)
            })
            .Where(arg => arg.Cents >= minTotalCents)
            .ToListAsync();
        return result.ToImmutableDictionary(arg => arg.UserId, arg => arg.Cents);
    }

    public async Task<SortedDictionary<DonationRecordBreakType, Donation>> GetRecordDonations(Instant now)
    {
        var result = new SortedDictionary<DonationRecordBreakType, Donation>();
        foreach (DonationRecordBreakType donationRecordBreakType in DonationRecordBreaks.Types)
        {
            Instant cutoff = now - donationRecordBreakType.Duration;
            Donation? recordDonation = await Collection.AsQueryable()
                .Where(donation => donation.CreatedAt > cutoff)
                .OrderByDescending(donation => donation.Cents).ThenBy(donation => donation.CreatedAt)
                .FirstOrDefaultAsync();
            if (recordDonation != null)
                result.Add(donationRecordBreakType, recordDonation);
        }
        return result;
    }
}
