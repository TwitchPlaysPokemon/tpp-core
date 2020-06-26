using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NodaTime;
using Persistence.Models;
using Persistence.Repos;

namespace Persistence.MongoDB.Repos
{
    public class UserRepo : IUserRepo
    {
        private const string CollectionName = "users";

        public readonly IMongoCollection<User> Collection;

        private readonly int _startingPokeyen;
        private readonly int _startingTokens;

        static UserRepo()
        {
            BsonClassMap.RegisterClassMap<User>(cm =>
            {
                cm.MapIdProperty(u => u.Id);
                cm.MapProperty(u => u.TwitchDisplayName).SetElementName("display_name");
                cm.MapProperty(u => u.Name).SetElementName("name");
                cm.MapProperty(u => u.SimpleName).SetElementName("name_lower");
                cm.MapProperty(u => u.Color).SetElementName("color");
                cm.MapProperty(u => u.FirstActiveAt).SetElementName("first_active_at");
                cm.MapProperty(u => u.LastActiveAt).SetElementName("last_active_at");
                cm.MapProperty(u => u.LastMessageAt).SetElementName("last_message_at");
                cm.MapProperty(u => u.Pokeyen).SetElementName("pokeyen");
                cm.MapProperty(u => u.Tokens).SetElementName("tokens");
                cm.MapProperty(u => u.ParticipationEmblems).SetElementName("participation");
                cm.MapProperty(u => u.SelectedParticipationEmblem).SetElementName("selected_participation_badge");
                cm.MapProperty(u => u.SelectedBadge).SetElementName("badge");
            });
        }

        public UserRepo(IMongoDatabase database, int startingPokeyen, int startingTokens)
        {
            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<User>(CollectionName);
            _startingPokeyen = startingPokeyen;
            _startingTokens = startingTokens;
            InitIndexes();
        }

        private void InitIndexes()
        {
            Collection.Indexes.CreateMany(new[]
            {
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.SimpleName)),
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Pokeyen)),
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Tokens)),
            });
        }

        public async Task<User> RecordUser(UserInfo userInfo)
        {
            UpdateDefinition<User> update = Builders<User>.Update
                .Set(u => u.TwitchDisplayName, userInfo.TwitchDisplayName)
                .Set(u => u.SimpleName, userInfo.SimpleName)
                .Set(u => u.Color, userInfo.Color)
                .Set(u => u.LastActiveAt, userInfo.UpdatedAt);
            if (userInfo.FromMessage)
            {
                update = update.Set(u => u.LastMessageAt, userInfo.UpdatedAt);
            }
            var user = await Collection.FindOneAndUpdateAsync<User>(
                filter: u => u.Id == userInfo.Id,
                update: update,
                options: new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After, IsUpsert = false }
            );
            if (user != null)
            {
                return user;
            }

            // user doesn't exist yet!

            user = new User(
                id: userInfo.Id,
                name: userInfo.SimpleName,
                twitchDisplayName: userInfo.TwitchDisplayName,
                simpleName: userInfo.SimpleName,
                color: userInfo.Color,
                firstActiveAt: userInfo.UpdatedAt,
                lastActiveAt: userInfo.UpdatedAt,
                lastMessageAt: userInfo.FromMessage ? userInfo.UpdatedAt : (Instant?)null,
                pokeyen: _startingPokeyen,
                tokens: _startingTokens
            );
            await Collection.InsertOneAsync(document: user);
            return user;
        }

        public async Task<User?> FindBySimpleName(string simpleName)
        {
            return await Collection.Find(u => u.SimpleName == simpleName).FirstOrDefaultAsync();
        }
    }
}
