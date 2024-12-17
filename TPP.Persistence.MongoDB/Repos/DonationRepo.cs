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

public class DonationRepo : IDonationRepo
{
    private const string CollectionName = "donations";

    public readonly IMongoCollection<Donation> Collection;

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

    public DonationRepo(IMongoDatabase database)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<Donation>(CollectionName);
    }

    public async Task<Donation?> FindDonation(int donationId) =>
        await Collection.Find(p => p.DonationId == donationId).FirstOrDefaultAsync();

    public async Task<Donation> InsertDonation(
        int donationId, Instant createdAt, string userName, string? userId, int cents, string? message = null)
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
}
