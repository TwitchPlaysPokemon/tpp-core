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
}
