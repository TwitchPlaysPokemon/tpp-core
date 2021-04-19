using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NodaTime;
using NUnit.Framework;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Repos;
using TPP.Persistence.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos
{
    [Parallelizable(ParallelScope.All)]
    public class UserRepoTest : MongoTestBase
    {
        private UserRepo CreateUserRepo()
        {
            IMongoDatabase database = CreateTemporaryDatabase();
            UserRepo userRepo = new UserRepo(database, 100, 1);
            Assert.AreEqual(expected: 0, actual: userRepo.Collection.CountDocuments(FilterDefinition<User>.Empty));
            return userRepo;
        }

        /// <summary>
        /// Tests that recording new data supplied in the <see cref="UserInfo"/> struct works properly.
        /// </summary>
        [Test]
        public async Task recording_user_updates_userinfo_data()
        {
            // given
            UserRepo userRepo = CreateUserRepo();
            const string userId = "123";
            const string displayNameBefore = "ユーザー名";
            const string displayNameAfter = "インマウィアブ";
            const string usernameBefore = "username";
            const string usernameAfter = "i_changed_my_name";

            // when
            User userBefore = await userRepo.RecordUser(new UserInfo(
                userId, twitchDisplayName: displayNameBefore, simpleName: usernameBefore,
                HexColor.FromWithHash("#123456")));
            User userAfter = await userRepo.RecordUser(new UserInfo(
                userId, twitchDisplayName: displayNameAfter, simpleName: usernameAfter,
                HexColor.FromWithHash("#abcdef")));

            // then
            Assert.AreEqual(1, await userRepo.Collection.CountDocumentsAsync(FilterDefinition<User>.Empty));
            Assert.AreEqual(userId, userBefore.Id);
            Assert.AreEqual(userId, userAfter.Id);
            Assert.AreEqual(displayNameBefore, userBefore.TwitchDisplayName);
            Assert.AreEqual(displayNameAfter, userAfter.TwitchDisplayName);
            Assert.AreEqual(usernameBefore, userBefore.SimpleName);
            Assert.AreEqual(usernameAfter, userAfter.SimpleName);
            Assert.AreEqual("123456", userBefore.Color);
            Assert.AreEqual("abcdef", userAfter.Color);
        }

        /// <summary>
        /// Tests that the default starting currencies get set properly, and don't get reset for existing users.
        /// </summary>
        [Test]
        public async Task recording_new_user_sets_starting_currency()
        {
            UserRepo userRepo = CreateUserRepo();
            // given
            var userInfo = new UserInfo("123", "X", "x", null);

            // when new user
            User userBefore = await userRepo.RecordUser(userInfo);
            // then initial currency
            Assert.AreEqual(100, userBefore.Pokeyen);
            Assert.AreEqual(1, userBefore.Tokens);

            // when existing user
            await userRepo.Collection.UpdateOneAsync(u => u.Id == userInfo.Id, Builders<User>.Update
                .Set(u => u.Pokeyen, 123)
                .Set(u => u.Tokens, 5));
            User userAfter = await userRepo.RecordUser(userInfo);
            // then keep currency
            Assert.AreEqual(123, userAfter.Pokeyen);
            Assert.AreEqual(5, userAfter.Tokens);
        }

        /// <summary>
        /// Tests that <see cref="User.LastActiveAt"/> gets updated properly.
        /// </summary>
        [Test]
        public async Task recording_user_updates_last_active_at()
        {
            UserRepo userRepo = CreateUserRepo();
            // given
            Instant t1 = Instant.FromUnixTimeSeconds(0);
            Instant t2 = Instant.FromUnixTimeSeconds(1);
            var userInfoT1 = new UserInfo("123", "X", "x", null, updatedAt: t1);
            var userInfoT2 = new UserInfo("123", "X", "x", null, updatedAt: t2);

            // when, then
            User userT1 = await userRepo.RecordUser(userInfoT1);
            Assert.AreEqual(t1, userT1.LastActiveAt);
            User userT2 = await userRepo.RecordUser(userInfoT2);
            Assert.AreEqual(t2, userT2.LastActiveAt);
        }

        /// <summary>
        /// Tests that <see cref="User.LastMessageAt"/> only gets updated if the <see cref="UserInfo.FromMessage"/>
        /// flag is set.
        /// </summary>
        [Test]
        public async Task recording_user_with_frommessage_flag_updates_last_message_at()
        {
            UserRepo userRepo = CreateUserRepo();
            // given
            Instant t1 = Instant.FromUnixTimeSeconds(0);
            Instant t2 = Instant.FromUnixTimeSeconds(1);
            Instant t3 = Instant.FromUnixTimeSeconds(2);
            var userInfoT1 = new UserInfo("123", "X", "x", null, updatedAt: t1);
            var userInfoT2 = new UserInfo("123", "X", "x", null, updatedAt: t2, fromMessage: true);
            var userInfoT3 = new UserInfo("123", "X", "x", null, updatedAt: t3);

            // when, then
            User userT1 = await userRepo.RecordUser(userInfoT1);
            Assert.AreEqual(null, userT1.LastMessageAt); // fromMessage=false, stayed null
            User userT2 = await userRepo.RecordUser(userInfoT2);
            Assert.AreEqual(t2, userT2.LastMessageAt); // fromMessage=true, got updated
            User userT3 = await userRepo.RecordUser(userInfoT3);
            Assert.AreEqual(t2, userT3.LastMessageAt); // fromMessage=false, didn't get updated
        }

        /// <summary>
        /// Tests that an initial property value specified in <see cref="User"/> gets saved for new entries,
        /// and gets properly overwritten if there's an actual value in the database.
        /// I use <see cref="User.ParticipationEmblems"/> for this case, whose initial value is an empty sorted set.
        /// </summary>
        [Test]
        public async Task recording_user_with_default_participation_reads_as_empty_set()
        {
            UserRepo userRepo = CreateUserRepo();
            // given
            var userInfo = new UserInfo("123", "X", "x", null);

            // when, then
            User userNew = await userRepo.RecordUser(userInfo);
            Assert.NotNull(userNew.ParticipationEmblems);
            Assert.AreEqual(new SortedSet<int>(), userNew.ParticipationEmblems);

            await userRepo.Collection.UpdateOneAsync(u => u.Id == userInfo.Id, Builders<User>.Update
                .Set(u => u.ParticipationEmblems, new SortedSet<int> { 42 }));
            // when, then
            User userExisting = await userRepo.RecordUser(userInfo);
            Assert.NotNull(userExisting.ParticipationEmblems);
            Assert.AreEqual(new SortedSet<int> { 42 }, userExisting.ParticipationEmblems);
        }

        /// <summary>
        /// Ensures that if the database does not contain the "participation" field,
        /// that it gets deserialized as an empty set instead of null.
        /// </summary>
        [Test]
        public async Task reading_user_without_participation_returns_as_empty_set()
        {
            UserRepo userRepo = CreateUserRepo();
            // given
            var userInfo = new UserInfo("123", "X", "x", null);
            await userRepo.RecordUser(userInfo);
            UpdateResult updateResult = await userRepo.Collection.UpdateOneAsync(u => u.Id == userInfo.Id,
                Builders<User>.Update.Unset(u => u.ParticipationEmblems));
            Assert.AreEqual(1, updateResult.ModifiedCount);

            // when
            User? deserializedUser = await userRepo.FindBySimpleName(userInfo.SimpleName);

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
        public async Task recording_users_concurrently_works_reliably()
        {
            UserRepo userRepo = CreateUserRepo();
            var userInfo = new UserInfo("123", "X", "x", null);
            await Task.WhenAll(Enumerable.Range(0, 100)
                .Select(i => userRepo.RecordUser(userInfo)));
        }

        [Test]
        public async Task supports_a_crazy_amount_of_money()
        {
            const long pokeyen = long.MaxValue - 123;
            const long tokens = long.MaxValue - 234;
            var userRepo = new UserRepo(CreateTemporaryDatabase(), pokeyen, tokens);

            User userFromRecording = await userRepo.RecordUser(new UserInfo("123", "X", "x", null));
            Assert.AreEqual(pokeyen, userFromRecording.Pokeyen);
            Assert.AreEqual(tokens, userFromRecording.Tokens);

            User? userFromReading = await userRepo.FindBySimpleName("x");
            Assert.NotNull(userFromReading);
            Assert.AreNotSame(userFromReading!, userFromRecording);
            Assert.AreEqual(pokeyen, userFromReading!.Pokeyen);
            Assert.AreEqual(tokens, userFromReading!.Tokens);
        }

        [Test]
        public async Task set_is_subscribed()
        {
            IUserRepo userRepo = new UserRepo(CreateTemporaryDatabase(), 0, 0);

            User userBeforeUpdate = await userRepo.RecordUser(new UserInfo("123", "X", "x"));
            Assert.IsFalse(userBeforeUpdate.IsSubscribed);
            User userAfterUpdate = await userRepo.SetIsSubscribed(userBeforeUpdate, true);
            Assert.IsTrue(userAfterUpdate.IsSubscribed);
        }

        [Test]
        public async Task set_subscription_info()
        {
            IUserRepo userRepo = new UserRepo(CreateTemporaryDatabase(), 0, 0);

            User userBeforeUpdate = await userRepo.RecordUser(new UserInfo("123", "X", "x"));
            Assert.AreEqual(0, userBeforeUpdate.MonthsSubscribed);
            Assert.IsNull(userBeforeUpdate.SubscriptionTier);
            Assert.AreEqual(0, userBeforeUpdate.LoyaltyLeague);
            Assert.IsNull(userBeforeUpdate.SubscriptionUpdatedAt);

            User userAfterUpdate = await userRepo.SetSubscriptionInfo(userBeforeUpdate,
                42, SubscriptionTier.Tier2, 10, Instant.FromUnixTimeSeconds(123));
            Assert.AreEqual(42, userAfterUpdate.MonthsSubscribed);
            Assert.AreEqual(SubscriptionTier.Tier2, userAfterUpdate.SubscriptionTier);
            Assert.AreEqual(10, userAfterUpdate.LoyaltyLeague);
            Assert.AreEqual(Instant.FromUnixTimeSeconds(123), userAfterUpdate.SubscriptionUpdatedAt);
        }
    }
}
