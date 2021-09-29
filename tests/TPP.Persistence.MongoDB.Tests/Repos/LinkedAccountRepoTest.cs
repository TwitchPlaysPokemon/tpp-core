using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Driver;
using NUnit.Framework;
using TPP.Model;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos;

public class LinkedAccountRepoTest : MongoTestBase
{
    [Test]
    public async Task link_accounts()
    {
        IMongoDatabase database = CreateTemporaryDatabase();
        UserRepo userRepo = new(database, 0, 0, ImmutableList<string>.Empty);
        User user1 = await userRepo.RecordUser(new UserInfo("user1", "User1", "user1"));
        User user2 = await userRepo.RecordUser(new UserInfo("user2", "User2", "user2"));
        User user3 = await userRepo.RecordUser(new UserInfo("user3", "User3", "user3"));
        ILinkedAccountRepo linkedAccountRepo = new LinkedAccountRepo(database, userRepo.Collection);

        // no initial links
        CollectionAssert.IsEmpty(await linkedAccountRepo.FindLinkedUsers("user1"));
        CollectionAssert.IsEmpty(await linkedAccountRepo.FindLinkedUsers("user2"));
        // successfully create links
        Assert.IsTrue(await linkedAccountRepo.Link(ImmutableHashSet.Create("user1", "user2")));
        List<User> links1And2 = new() { user1, user2 };
        CollectionAssert.AreEquivalent(links1And2, await linkedAccountRepo.FindLinkedUsers("user1"));
        CollectionAssert.AreEquivalent(links1And2, await linkedAccountRepo.FindLinkedUsers("user2"));
        Assert.IsTrue(await linkedAccountRepo.AreLinked("user1", "user2"));
        // links already exist
        Assert.IsFalse(await linkedAccountRepo.Link(ImmutableHashSet.Create("user1", "user2")));
        CollectionAssert.AreEquivalent(links1And2, await linkedAccountRepo.FindLinkedUsers("user1"));
        CollectionAssert.AreEquivalent(links1And2, await linkedAccountRepo.FindLinkedUsers("user2"));
        Assert.IsTrue(await linkedAccountRepo.AreLinked("user1", "user2"));
        // link undone
        Assert.IsTrue(await linkedAccountRepo.Unlink("user1"));
        CollectionAssert.IsEmpty(await linkedAccountRepo.FindLinkedUsers("user1"));
        CollectionAssert.IsEmpty(await linkedAccountRepo.FindLinkedUsers("user2"));
        Assert.IsFalse(await linkedAccountRepo.AreLinked("user1", "user2"));
        // already not linked
        Assert.IsFalse(await linkedAccountRepo.Unlink("user2"));
        CollectionAssert.IsEmpty(await linkedAccountRepo.FindLinkedUsers("user1"));
        CollectionAssert.IsEmpty(await linkedAccountRepo.FindLinkedUsers("user2"));
        Assert.IsFalse(await linkedAccountRepo.AreLinked("user1", "user2"));
        // other users linked
        Assert.IsTrue(await linkedAccountRepo.Link(ImmutableHashSet.Create("user1", "user3")));
        List<User> links1And3 = new() { user1, user3 };
        CollectionAssert.AreEquivalent(links1And3, await linkedAccountRepo.FindLinkedUsers("user1"));
        CollectionAssert.IsEmpty(await linkedAccountRepo.FindLinkedUsers("user2"));
        Assert.IsFalse(await linkedAccountRepo.AreLinked("user1", "user2"));
        Assert.IsTrue(await linkedAccountRepo.AreLinked("user1", "user3"));
        // user1=user3 and user2=user3 implies user1=user2
        Assert.IsTrue(await linkedAccountRepo.Link(ImmutableHashSet.Create("user2", "user3")));
        Assert.IsTrue(await linkedAccountRepo.AreLinked("user2", "user3"));
        Assert.IsTrue(await linkedAccountRepo.AreLinked("user1", "user2"));
        // links from user1 undone, user2 and user3 are still linked
        Assert.IsTrue(await linkedAccountRepo.Unlink("user1"));
        Assert.IsTrue(await linkedAccountRepo.AreLinked("user2", "user3"));
        CollectionAssert.IsEmpty(await linkedAccountRepo.FindLinkedUsers("user1"));
    }
}
