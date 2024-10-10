using System.Threading.Tasks;
using NUnit.Framework;
using PersistenceMongoDB.Repos;

namespace PersistenceMongoDB.Tests.Repos;

public class RunCounterRepoTest : MongoTestBase
{
    [Test]
    public async Task increment_global_counter()
    {
        RunCounterRepo repo = new(CreateTemporaryDatabase());
        Assert.That(await repo.Get(null), Is.EqualTo(0));
        long newCounter1 = await repo.Increment(null);
        Assert.That(newCounter1, Is.EqualTo(1));
        Assert.That(await repo.Get(null), Is.EqualTo(1));
        long newCounter2 = await repo.Increment(null, incrementBy: 99);
        Assert.That(newCounter2, Is.EqualTo(100));
        Assert.That(await repo.Get(null), Is.EqualTo(100));
        Assert.That(await repo.Get(123), Is.EqualTo(0));
    }

    [Test]
    public async Task increment_run_counter()
    {
        // implicitly also increments the global counter
        int runNumber = 123;
        RunCounterRepo repo = new(CreateTemporaryDatabase());
        Assert.That(await repo.Get(runNumber), Is.EqualTo(0));
        Assert.That(await repo.Get(null), Is.EqualTo(0));
        long newCounter1 = await repo.Increment(runNumber);
        Assert.That(newCounter1, Is.EqualTo(1));
        Assert.That(await repo.Get(runNumber), Is.EqualTo(1));
        Assert.That(await repo.Get(null), Is.EqualTo(1));
        long newCounter2 = await repo.Increment(runNumber, incrementBy: 99);
        Assert.That(newCounter2, Is.EqualTo(100));
        Assert.That(await repo.Get(runNumber), Is.EqualTo(100));
        Assert.That(await repo.Get(null), Is.EqualTo(100));
    }
}
