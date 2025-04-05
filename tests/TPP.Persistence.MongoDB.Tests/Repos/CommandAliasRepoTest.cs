using System;
using System.Threading.Tasks;
using NUnit.Framework;
using TPP.Model;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos;

public class CommandAliasRepoTest : MongoTestBase
{
    [Test]
    public async Task persists_and_deletes_successfully()
    {
        CommandAliasRepo repo = new(CreateTemporaryDatabase());
        Assert.That(await repo.GetAliases(), Is.Empty);

        CommandAlias alias1 = await repo.UpsertAlias("Alias1", "target1", []);
        CommandAlias alias2 = await repo.UpsertAlias("alias2", "target2", ["foo", "bar"]);
        Assert.That(await repo.GetAliases(), Is.EquivalentTo([alias1, alias2]));

        Assert.That(await repo.RemoveAlias("ALIAS1"), Is.True);
        Assert.That(await repo.RemoveAlias("alias1"), Is.False); // already deleted
        Assert.That(await repo.GetAliases(), Is.EquivalentTo([alias2]));
    }

    [Test]
    public async Task updates_existing()
    {
        CommandAliasRepo repo = new(CreateTemporaryDatabase());
        Assert.That(await repo.GetAliases(), Is.Empty);

        CommandAlias alias1 = await repo.UpsertAlias("alias", "target1", []);
        Assert.That(await repo.GetAliases(), Is.EquivalentTo([alias1]));
        CommandAlias alias2 = await repo.UpsertAlias("ALIAS", "target2", ["foo"]);
        Assert.That(await repo.GetAliases(), Is.EquivalentTo([alias2]));
    }

    [Test]
    public async Task rejects_spaces_in_target()
    {
        CommandAliasRepo repo = new(CreateTemporaryDatabase());
        Assert.That(await repo.GetAliases(), Is.Empty);

        Assert.ThrowsAsync<ArgumentException>(() => repo.UpsertAlias("alias", "target with spaces", []));
    }
}
