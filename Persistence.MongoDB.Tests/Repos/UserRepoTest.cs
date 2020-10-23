using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NodaTime;
using NUnit.Framework;
using Persistence.Models;
using Persistence.MongoDB.Repos;
using Persistence.MongoDB.Serializers;
using Persistence.Repos;

namespace Persistence.MongoDB.Tests.Repos
{
    [Category("IntegrationTest")]
    public class UserRepoTest : MongoTestBase
    {
        private UserRepo _userRepo = null!;

        [OneTimeSetUp]
        public void SetUpSerializers() => CustomSerializers.RegisterAll();

        [SetUp]
        public void SetUp()
        {
            IMongoDatabase database = CreateTemporaryDatabase();
            _userRepo = new UserRepo(database, 100, 1);
            Assert.AreEqual(expected: 0, actual: _userRepo.Collection.CountDocuments(FilterDefinition<User>.Empty));
        }

        /// <summary>
        /// Tests that recording new data supplied in the <see cref="UserInfo"/> struct works properly.
        /// </summary>
        [Test]
        public async Task TestRecordUser()
        {
            // given
            const string userId = "123";
            const string displayNameBefore = "ユーザー名";
            const string displayNameAfter = "インマウィアブ";
            const string usernameBefore = "username";
            const string usernameAfter = "i_changed_my_name";

            // when
            User userBefore = await _userRepo.RecordUser(new UserInfo(
                userId, twitchDisplayName: displayNameBefore, simpleName: usernameBefore, color: "foo"));
            User userAfter = await _userRepo.RecordUser(new UserInfo(
                userId, twitchDisplayName: displayNameAfter, simpleName: usernameAfter, color: "bar"));

            // then
            Assert.AreEqual(expected: 1, actual: _userRepo.Collection.CountDocuments(FilterDefinition<User>.Empty));
            Assert.AreEqual(expected: userId, actual: userBefore.Id);
            Assert.AreEqual(expected: userId, actual: userAfter.Id);
            Assert.AreEqual(expected: displayNameBefore, actual: userBefore.TwitchDisplayName);
            Assert.AreEqual(expected: displayNameAfter, actual: userAfter.TwitchDisplayName);
            Assert.AreEqual(expected: usernameBefore, actual: userBefore.SimpleName);
            Assert.AreEqual(expected: usernameAfter, actual: userAfter.SimpleName);
        }

        /// <summary>
        /// Tests that the default starting currencies get set properly, and don't get reset for existing users.
        /// </summary>
        [Test]
        public async Task TestStartingCurrency()
        {
            // given
            var userInfo = new UserInfo("123", "X", "x", null);

            // when new user
            User userBefore = await _userRepo.RecordUser(userInfo);
            // then initial currency
            Assert.AreEqual(100, userBefore.Pokeyen);
            Assert.AreEqual(1, userBefore.Tokens);

            // when existing user
            await _userRepo.Collection.UpdateOneAsync(u => u.Id == userInfo.Id, Builders<User>.Update
                .Set(u => u.Pokeyen, 123)
                .Set(u => u.Tokens, 5));
            User userAfter = await _userRepo.RecordUser(userInfo);
            // then keep currency
            Assert.AreEqual(123, userAfter.Pokeyen);
            Assert.AreEqual(5, userAfter.Tokens);
        }

        /// <summary>
        /// Tests that <see cref="User.LastActiveAt"/> gets updated properly.
        /// </summary>
        [Test]
        public async Task TestUpdateLastActiveAt()
        {
            // given
            Instant t1 = Instant.FromUnixTimeSeconds(0);
            Instant t2 = Instant.FromUnixTimeSeconds(1);
            var userInfoT1 = new UserInfo("123", "X", "x", null, updatedAt: t1);
            var userInfoT2 = new UserInfo("123", "X", "x", null, updatedAt: t2);

            // when, then
            User userT1 = await _userRepo.RecordUser(userInfoT1);
            Assert.AreEqual(t1, userT1.LastActiveAt);
            User userT2 = await _userRepo.RecordUser(userInfoT2);
            Assert.AreEqual(t2, userT2.LastActiveAt);
        }

        /// <summary>
        /// Tests that <see cref="User.LastMessageAt"/> only gets updated if the <see cref="UserInfo.FromMessage"/>
        /// flag is set.
        /// </summary>
        [Test]
        public async Task TestUpdateLastMessageAt()
        {
            // given
            Instant t1 = Instant.FromUnixTimeSeconds(0);
            Instant t2 = Instant.FromUnixTimeSeconds(1);
            Instant t3 = Instant.FromUnixTimeSeconds(2);
            var userInfoT1 = new UserInfo("123", "X", "x", null, updatedAt: t1);
            var userInfoT2 = new UserInfo("123", "X", "x", null, updatedAt: t2, fromMessage: true);
            var userInfoT3 = new UserInfo("123", "X", "x", null, updatedAt: t3);

            // when, then
            User userT1 = await _userRepo.RecordUser(userInfoT1);
            Assert.AreEqual(null, userT1.LastMessageAt); // fromMessage=false, stayed null
            User userT2 = await _userRepo.RecordUser(userInfoT2);
            Assert.AreEqual(t2, userT2.LastMessageAt); // fromMessage=true, got updated
            User userT3 = await _userRepo.RecordUser(userInfoT3);
            Assert.AreEqual(t2, userT3.LastMessageAt); // fromMessage=false, didn't get updated
        }

        /// <summary>
        /// Tests that an initial property value specified in <see cref="User"/> gets saved for new entries,
        /// and gets properly overwritten if there's an actual value in the database.
        /// I use <see cref="User.ParticipationEmblems"/> for this case, whose initial value is an empty sorted set.
        /// </summary>
        [Test]
        public async Task TestUseDefaultEmptySet()
        {
            // given
            var userInfo = new UserInfo("123", "X", "x", null);

            // when, then
            User userNew = await _userRepo.RecordUser(userInfo);
            Assert.NotNull(userNew.ParticipationEmblems);
            Assert.AreEqual(new SortedSet<int>(), userNew.ParticipationEmblems);

            await _userRepo.Collection.UpdateOneAsync(u => u.Id == userInfo.Id, Builders<User>.Update
                .Set(u => u.ParticipationEmblems, new SortedSet<int> { 42 }));
            // when, then
            User userExisting = await _userRepo.RecordUser(userInfo);
            Assert.NotNull(userExisting.ParticipationEmblems);
            Assert.AreEqual(new SortedSet<int> { 42 }, userExisting.ParticipationEmblems);
        }

        /// <summary>
        /// Ensures that if the database does not contain the "participation" field,
        /// that it gets deserialized as an empty set instead of null.
        /// </summary>
        [Test]
        public async Task TestDeserializeMissingParticipationAsEmpty()
        {
            // given
            var userInfo = new UserInfo("123", "X", "x", null);
            await _userRepo.RecordUser(userInfo);
            UpdateResult updateResult = await _userRepo.Collection.UpdateOneAsync(u => u.Id == userInfo.Id,
                    Builders<User>.Update.Unset(u => u.ParticipationEmblems));
            Assert.AreEqual(1, updateResult.ModifiedCount);

            // when
            User? deserializedUser = await _userRepo.FindBySimpleName(userInfo.SimpleName);

            // then
            Assert.NotNull(deserializedUser);
            Assert.NotNull(deserializedUser!.ParticipationEmblems);
            Assert.AreEqual(new SortedSet<int>(), deserializedUser!.ParticipationEmblems);
        }

        /// <summary>
        /// Tests that concurrent user recordings work reliably and do not cause
        /// "E11000 duplicate key error collection" errors or similar.
        /// </summary>
        [Test]
        public async Task TestRecordUserConcurrently()
        {
            var userInfo = new UserInfo("123", "X", "x", null);
            await Task.WhenAll(Enumerable.Range(0, 100)
                .Select(i => _userRepo.RecordUser(userInfo)));
        }
    }
}
