using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.Common;
using TPP.Model;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos
{
    [Parallelizable(ParallelScope.All)]
    public class UserRepoTest : MongoTestBase
    {
        private UserRepo CreateUserRepo()
        {
            IMongoDatabase database = CreateTemporaryDatabase();
            UserRepo userRepo = new UserRepo(database, 100, 1, ImmutableList<string>.Empty, Mock.Of<IClock>());
            Assert.That(userRepo.Collection.CountDocuments(FilterDefinition<User>.Empty), Is.EqualTo(expected: 0));
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
                userId, TwitchDisplayName: displayNameBefore, SimpleName: usernameBefore,
                HexColor.FromWithHash("#123456")));
            User userAfter = await userRepo.RecordUser(new UserInfo(
                userId, TwitchDisplayName: displayNameAfter, SimpleName: usernameAfter,
                HexColor.FromWithHash("#abcdef")));

            // then
            Assert.That(await userRepo.Collection.CountDocumentsAsync(FilterDefinition<User>.Empty), Is.EqualTo(1));
            Assert.That(userBefore.Id, Is.EqualTo(userId));
            Assert.That(userAfter.Id, Is.EqualTo(userId));
            Assert.That(userBefore.TwitchDisplayName, Is.EqualTo(displayNameBefore));
            Assert.That(userAfter.TwitchDisplayName, Is.EqualTo(displayNameAfter));
            Assert.That(userBefore.SimpleName, Is.EqualTo(usernameBefore));
            Assert.That(userAfter.SimpleName, Is.EqualTo(usernameAfter));
            Assert.That(userBefore.Color, Is.EqualTo("123456"));
            Assert.That(userAfter.Color, Is.EqualTo("abcdef"));
        }

        /// <summary>
        /// Tests that the default starting currencies get set properly, and don't get reset for existing users.
        /// </summary>
        [Test]
        public async Task recording_new_user_sets_starting_currency()
        {
            UserRepo userRepo = CreateUserRepo();
            // given
            var userInfo = new UserInfo("123", "X", "x");

            // when new user
            User userBefore = await userRepo.RecordUser(userInfo);
            // then initial currency
            Assert.That(userBefore.Pokeyen, Is.EqualTo(100));
            Assert.That(userBefore.Tokens, Is.EqualTo(1));

            // when existing user
            await userRepo.Collection.UpdateOneAsync(u => u.Id == userInfo.Id, Builders<User>.Update
                .Set(u => u.Pokeyen, 123)
                .Set(u => u.Tokens, 5));
            User userAfter = await userRepo.RecordUser(userInfo);
            // then keep currency
            Assert.That(userAfter.Pokeyen, Is.EqualTo(123));
            Assert.That(userAfter.Tokens, Is.EqualTo(5));
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
            var userInfoT1 = new UserInfo("123", "X", "x", null, UpdatedAt: t1);
            var userInfoT2 = new UserInfo("123", "X", "x", null, UpdatedAt: t2);

            // when, then
            User userT1 = await userRepo.RecordUser(userInfoT1);
            Assert.That(userT1.LastActiveAt, Is.EqualTo(t1));
            User userT2 = await userRepo.RecordUser(userInfoT2);
            Assert.That(userT2.LastActiveAt, Is.EqualTo(t2));
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
            var userInfoT1 = new UserInfo("123", "X", "x", null, UpdatedAt: t1);
            var userInfoT2 = new UserInfo("123", "X", "x", null, UpdatedAt: t2, FromMessage: true);
            var userInfoT3 = new UserInfo("123", "X", "x", null, UpdatedAt: t3);

            // when, then
            User userT1 = await userRepo.RecordUser(userInfoT1);
            Assert.That(userT1.LastMessageAt, Is.EqualTo(null)); // fromMessage=false, stayed null
            User userT2 = await userRepo.RecordUser(userInfoT2);
            Assert.That(userT2.LastMessageAt, Is.EqualTo(t2)); // fromMessage=true, got updated
            User userT3 = await userRepo.RecordUser(userInfoT3);
            Assert.That(userT3.LastMessageAt, Is.EqualTo(t2)); // fromMessage=false, didn't get updated
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
            var userInfo = new UserInfo("123", "X", "x");

            // when, then
            User userNew = await userRepo.RecordUser(userInfo);
            Assert.NotNull(userNew.ParticipationEmblems);
            Assert.That(userNew.ParticipationEmblems, Is.EqualTo(new SortedSet<int>()));

            await userRepo.Collection.UpdateOneAsync(u => u.Id == userInfo.Id, Builders<User>.Update
                .Set(u => u.ParticipationEmblems, new SortedSet<int> { 42 }));
            // when, then
            User userExisting = await userRepo.RecordUser(userInfo);
            Assert.NotNull(userExisting.ParticipationEmblems);
            Assert.That(userExisting.ParticipationEmblems, Is.EqualTo(new SortedSet<int> { 42 }));
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
            var userInfo = new UserInfo("123", "X", "x");
            await userRepo.RecordUser(userInfo);
            UpdateResult updateResult = await userRepo.Collection.UpdateOneAsync(u => u.Id == userInfo.Id,
                Builders<User>.Update.Unset(u => u.ParticipationEmblems));
            Assert.That(updateResult.ModifiedCount, Is.EqualTo(1));

            // when
            User? deserializedUser = await userRepo.FindBySimpleName(userInfo.SimpleName);

            // then
            Assert.NotNull(deserializedUser);
            Assert.NotNull(deserializedUser!.ParticipationEmblems);
            Assert.That(deserializedUser.ParticipationEmblems, Is.EqualTo(new SortedSet<int>()));
        }

        /// <summary>
        /// Ensures that handing out participation emblems works properly.
        /// </summary>
        [Test]
        public async Task adding_participation_badge()
        {
            UserRepo userRepo = CreateUserRepo();
            // given
            var userInfo = new UserInfo("123", "X", "x");
            User user0Emblems = await userRepo.RecordUser(userInfo);
            Assert.That(user0Emblems.ParticipationEmblems, Is.Empty);
            User? userFromDb0Emblems = await userRepo.FindBySimpleName(userInfo.SimpleName);
            Assert.That(userFromDb0Emblems?.ParticipationEmblems, Is.Empty);

            // when, then
            User user1Emblem = await userRepo.GiveEmblem(user0Emblems, 1);
            Assert.That(user1Emblem.ParticipationEmblems, Is.EquivalentTo(new[] { 1 }));
            User? userFromDb1Emblem = await userRepo.FindBySimpleName(userInfo.SimpleName);
            Assert.That(userFromDb1Emblem?.ParticipationEmblems, Is.EquivalentTo(new[] { 1 }));

            // when, then
            User user2Emblems = await userRepo.GiveEmblem(user1Emblem, 2);
            Assert.That(user2Emblems.ParticipationEmblems, Is.EquivalentTo(new[] { 1, 2 }));
            User? userFromDb2Emblems = await userRepo.FindBySimpleName(userInfo.SimpleName);
            Assert.That(userFromDb2Emblems?.ParticipationEmblems, Is.EquivalentTo(new[] { 1, 2 }));
        }

        /// <summary>
        /// Tests that concurrent user recordings work reliably and do not cause
        /// "E11000 duplicate key error collection" errors or similar.
        /// </summary>
        [Test]
        public async Task recording_users_concurrently_works_reliably()
        {
            UserRepo userRepo = CreateUserRepo();
            var userInfo = new UserInfo("123", "X", "x");
            await Task.WhenAll(Enumerable.Range(0, 100)
                .Select(_ => userRepo.RecordUser(userInfo)));
        }

        [Test]
        public async Task supports_a_crazy_amount_of_money()
        {
            const long pokeyen = long.MaxValue - 123;
            const long tokens = long.MaxValue - 234;
            var userRepo = new UserRepo(
                CreateTemporaryDatabase(), pokeyen, tokens, ImmutableList<string>.Empty, Mock.Of<IClock>());

            User userFromRecording = await userRepo.RecordUser(new UserInfo("123", "X", "x"));
            Assert.That(userFromRecording.Pokeyen, Is.EqualTo(pokeyen));
            Assert.That(userFromRecording.Tokens, Is.EqualTo(tokens));

            User? userFromReading = await userRepo.FindBySimpleName("x");
            Assert.NotNull(userFromReading);
            Assert.That(userFromRecording, Is.Not.SameAs(userFromReading!));
            Assert.That(userFromReading!.Pokeyen, Is.EqualTo(pokeyen));
            Assert.That(userFromReading!.Tokens, Is.EqualTo(tokens));
        }

        [Test]
        public async Task set_is_subscribed()
        {
            IUserRepo userRepo = new UserRepo(
                CreateTemporaryDatabase(), 0, 0, ImmutableList<string>.Empty, Mock.Of<IClock>());

            User userBeforeUpdate = await userRepo.RecordUser(new UserInfo("123", "X", "x"));
            Assert.IsFalse(userBeforeUpdate.IsSubscribed);
            User userAfterUpdate = await userRepo.SetIsSubscribed(userBeforeUpdate, true);
            Assert.IsTrue(userAfterUpdate.IsSubscribed);
        }

        [Test]
        public async Task set_subscription_info()
        {
            IUserRepo userRepo = new UserRepo(
                CreateTemporaryDatabase(), 0, 0, ImmutableList<string>.Empty, Mock.Of<IClock>());

            User userBeforeUpdate = await userRepo.RecordUser(new UserInfo("123", "X", "x"));
            Assert.That(userBeforeUpdate.MonthsSubscribed, Is.EqualTo(0));
            Assert.IsNull(userBeforeUpdate.SubscriptionTier);
            Assert.That(userBeforeUpdate.LoyaltyLeague, Is.EqualTo(0));
            Assert.IsNull(userBeforeUpdate.SubscriptionUpdatedAt);

            User userAfterUpdate = await userRepo.SetSubscriptionInfo(userBeforeUpdate,
                42, SubscriptionTier.Tier2, 10, Instant.FromUnixTimeSeconds(123));
            Assert.That(userAfterUpdate.MonthsSubscribed, Is.EqualTo(42));
            Assert.That(userAfterUpdate.SubscriptionTier, Is.EqualTo(SubscriptionTier.Tier2));
            Assert.That(userAfterUpdate.LoyaltyLeague, Is.EqualTo(10));
            Assert.That(userAfterUpdate.SubscriptionUpdatedAt, Is.EqualTo(Instant.FromUnixTimeSeconds(123)));
        }

        /// <summary>
        /// Tests that if old core happens to save a bare document,
        /// new core can successfully understand it and interpret missing fields as sensible defaults.
        /// </summary>
        [Test]
        public async Task deserialize_empty_user_document_gives_defaults()
        {
            IMongoDatabase db = CreateTemporaryDatabase();
            IMongoCollection<BsonDocument> coll = db.GetCollection<BsonDocument>(UserRepo.CollectionName);

            await coll.InsertOneAsync(new BsonDocument { ["_id"] = "userid", ["name_lower"] = "username" });

            IUserRepo userRepo = new UserRepo(db, 0, 0, ImmutableList<string>.Empty, Mock.Of<IClock>());
            User? user = await userRepo.FindBySimpleName("username");
            Assert.That(user, Is.Not.Null);
            Assert.That(user?.Banned, Is.False);
            Assert.That(user?.TimeoutExpiration, Is.Null);
            Assert.That(user?.Pokeyen, Is.EqualTo(0));
            Assert.That(user?.PokeyenHighScore, Is.EqualTo(0));
            Assert.That(user?.Tokens, Is.EqualTo(0));
            Assert.That(user?.Roles.Count, Is.EqualTo(0));
            Assert.That(user?.ParticipationEmblems.Count, Is.EqualTo(0));
            Assert.That(user?.IsSubscribed, Is.False);
            Assert.That(user?.LoyaltyLeague, Is.EqualTo(0));
            Assert.That(user?.MonthsSubscribed, Is.EqualTo(0));
            Assert.That(user?.GlowColorUnlocked, Is.False);
        }
    }
}
