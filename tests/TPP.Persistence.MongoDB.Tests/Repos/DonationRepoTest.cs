using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NodaTime;
using NUnit.Framework;
using TPP.Model;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos;

public class DonationRepoTest : MongoTestBase
{
    [Test]
    public async Task insert_then_read_donation_works()
    {
        DonationRepo repo = new(CreateTemporaryDatabase());

        Donation inserted = await repo.InsertDonation(
            donationId: 1234,
            createdAt: Instant.FromUnixTimeSeconds(123),
            "username",
            "userid",
            42000,
            "Here's $420 yolo lol");

        Donation? read = await repo.FindDonation(1234);
        Assert.That(read, Is.Not.Null);
        Assert.That(read!, Is.Not.SameAs(inserted));
        Assert.That(read!, Is.EqualTo(inserted));

        Assert.That(read.DonationId, Is.EqualTo(1234));
        Assert.That(read.CreatedAt, Is.EqualTo(Instant.FromUnixTimeSeconds(123)));
        Assert.That(read.UserName, Is.EqualTo("username"));
        Assert.That(read.Cents, Is.EqualTo(42000));
        Assert.That(read.Message, Is.EqualTo("Here's $420 yolo lol"));
    }

    [Test]
    public async Task calc_cents_per_user()
    {
        DonationRepo repo = new(CreateTemporaryDatabase());

        int idCounter = 0;
        await repo.InsertDonation(++idCounter, Instant.FromUnixTimeSeconds(0), "random-username",
            userId: "user1", cents: 50);
        await repo.InsertDonation(++idCounter, Instant.FromUnixTimeSeconds(0), "random-username",
            userId: "user2", cents: 100);
        await repo.InsertDonation(++idCounter, Instant.FromUnixTimeSeconds(0), "random-username",
            userId: "user3", cents: 70);
        await repo.InsertDonation(++idCounter, Instant.FromUnixTimeSeconds(0), "random-username",
            userId: "user3", cents: 80);

        IImmutableDictionary<string, long> centsPerUserAll = await repo.GetCentsPerUser();
        IImmutableDictionary<string, long> centsPerUserOnlyUser1And2 =
            await repo.GetCentsPerUser(userIdFilter: new HashSet<string> { "user1", "user2" });
        IImmutableDictionary<string, long> centsPerUserMinCents = await repo.GetCentsPerUser(minTotalCents: 100);

        Assert.That(centsPerUserAll.Keys, Is.EquivalentTo(["user1", "user2", "user3"]));
        Assert.That(centsPerUserOnlyUser1And2.Keys, Is.EquivalentTo(["user1", "user2"]));
        Assert.That(centsPerUserOnlyUser1And2.Values, Is.EquivalentTo([50, 100]));
        Assert.That(centsPerUserMinCents.Keys, Is.EquivalentTo(["user2", "user3"]));
        Assert.That(centsPerUserMinCents.Values, Is.EquivalentTo([100, 150]));

        Assert.That(centsPerUserAll["user1"], Is.EqualTo(50));
        Assert.That(centsPerUserAll["user2"], Is.EqualTo(100));
        Assert.That(centsPerUserAll["user3"], Is.EqualTo(150));
        Assert.That(centsPerUserOnlyUser1And2["user1"], Is.EqualTo(50));
        Assert.That(centsPerUserOnlyUser1And2["user2"], Is.EqualTo(100));
        Assert.That(centsPerUserMinCents["user2"], Is.EqualTo(100));
        Assert.That(centsPerUserMinCents["user3"], Is.EqualTo(150));
    }
}
