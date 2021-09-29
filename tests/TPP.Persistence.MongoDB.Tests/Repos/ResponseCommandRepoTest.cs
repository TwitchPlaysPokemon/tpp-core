using System.Threading.Tasks;
using NUnit.Framework;
using TPP.Model;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos;

public class ResponseCommandRepoTest : MongoTestBase
{
    [Test]
    public async Task persists_and_deletes_successfully()
    {
        ResponseCommandRepo repo = new(CreateTemporaryDatabase());
        Assert.That(await repo.GetCommands(), Is.Empty);

        ResponseCommand command1 = await repo.UpsertCommand("command1", "response 1");
        ResponseCommand command2 = await repo.UpsertCommand("command2", "response 2");
        Assert.That(await repo.GetCommands(), Is.EquivalentTo(new[] { command1, command2 }));

        Assert.That(await repo.RemoveCommand("command1"), Is.True);
        Assert.That(await repo.RemoveCommand("command1"), Is.False); // already deleted
        Assert.That(await repo.GetCommands(), Is.EquivalentTo(new[] { command2 }));
    }

    [Test]
    public async Task updates_existing()
    {
        ResponseCommandRepo repo = new(CreateTemporaryDatabase());
        Assert.That(await repo.GetCommands(), Is.Empty);

        ResponseCommand command1 = await repo.UpsertCommand("command", "response 1");
        Assert.That(await repo.GetCommands(), Is.EquivalentTo(new[] { command1 }));
        ResponseCommand command2 = await repo.UpsertCommand("command", "response 2");
        Assert.That(await repo.GetCommands(), Is.EquivalentTo(new[] { command2 }));
    }
}
