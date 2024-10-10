using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Driver;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using Model;
using Persistence;
using PersistenceMongoDB.Repos;

namespace PersistenceMongoDB.Tests.Repos
{
    public class LinkedAccountRepoTest : MongoTestBase
    {
        [Test]
        public async Task link_accounts()
        {
            IMongoDatabase database = CreateTemporaryDatabase();
            UserRepo userRepo = new(database, 0, 0, ImmutableList<string>.Empty, Substitute.For<IClock>());
            User user1 = await userRepo.RecordUser(new UserInfo("user1", "User1", "user1"));
            User user2 = await userRepo.RecordUser(new UserInfo("user2", "User2", "user2"));
            User user3 = await userRepo.RecordUser(new UserInfo("user3", "User3", "user3"));
            ILinkedAccountRepo linkedAccountRepo = new LinkedAccountRepo(database, userRepo.Collection);

            // no initial links
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user1"), Is.Empty);
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user2"), Is.Empty);
            // successfully create links
            Assert.That(await linkedAccountRepo.Link(ImmutableHashSet.Create("user1", "user2")), Is.True);
            List<User> links1And2 = new() { user1, user2 };
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user1"), Is.EquivalentTo(links1And2));
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user2"), Is.EquivalentTo(links1And2));
            Assert.That(await linkedAccountRepo.AreLinked("user1", "user2"), Is.True);
            // links already exist
            Assert.That(await linkedAccountRepo.Link(ImmutableHashSet.Create("user1", "user2")), Is.False);
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user1"), Is.EquivalentTo(links1And2));
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user2"), Is.EquivalentTo(links1And2));
            Assert.That(await linkedAccountRepo.AreLinked("user1", "user2"), Is.True);
            // link undone
            Assert.That(await linkedAccountRepo.Unlink("user1"), Is.True);
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user1"), Is.Empty);
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user2"), Is.Empty);
            Assert.That(await linkedAccountRepo.AreLinked("user1", "user2"), Is.False);
            // already not linked
            Assert.That(await linkedAccountRepo.Unlink("user2"), Is.False);
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user1"), Is.Empty);
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user2"), Is.Empty);
            Assert.That(await linkedAccountRepo.AreLinked("user1", "user2"), Is.False);
            // other users linked
            Assert.That(await linkedAccountRepo.Link(ImmutableHashSet.Create("user1", "user3")), Is.True);
            List<User> links1And3 = new() { user1, user3 };
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user1"), Is.EquivalentTo(links1And3));
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user2"), Is.Empty);
            Assert.That(await linkedAccountRepo.AreLinked("user1", "user2"), Is.False);
            Assert.That(await linkedAccountRepo.AreLinked("user1", "user3"), Is.True);
            // user1=user3 and user2=user3 implies user1=user2
            Assert.That(await linkedAccountRepo.Link(ImmutableHashSet.Create("user2", "user3")), Is.True);
            Assert.That(await linkedAccountRepo.AreLinked("user2", "user3"), Is.True);
            Assert.That(await linkedAccountRepo.AreLinked("user1", "user2"), Is.True);
            // links from user1 undone, user2 and user3 are still linked
            Assert.That(await linkedAccountRepo.Unlink("user1"), Is.True);
            Assert.That(await linkedAccountRepo.AreLinked("user2", "user3"), Is.True);
            Assert.That(await linkedAccountRepo.FindLinkedUsers("user1"), Is.Empty);
        }
    }
}
