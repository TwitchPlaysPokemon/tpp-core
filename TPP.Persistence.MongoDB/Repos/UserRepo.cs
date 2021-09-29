using System.Linq.Expressions;
using TPP.Common;

namespace TPP.Persistence.MongoDB.Repos;

public class UserRepo : IUserRepo
{
    public const string CollectionName = "users";

    public readonly IMongoCollection<User> Collection;

    private readonly long _startingPokeyen;
    private readonly long _startingTokens;
    private readonly ImmutableHashSet<string> _defaultOperators;
    private readonly IClock _clock;

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
            cm.MapProperty(u => u.PokeyenHighScore).SetElementName("pokeyen_highscore")
                .SetDefaultValue(0L)
                .SetIgnoreIfDefault(true);
            cm.MapProperty(u => u.Tokens).SetElementName("tokens");
            cm.MapProperty(u => u.ParticipationEmblems)
                .SetElementName("participation")
                // behaviour of old core: omit if no participation.
                .SetDefaultValue(new SortedSet<int>())
                .SetIgnoreIfDefault(true);
            cm.MapProperty(u => u.SelectedParticipationEmblem).SetElementName("selected_participation_badge");
            cm.MapProperty(u => u.SelectedBadge).SetElementName("badge");
            cm.MapProperty(u => u.GlowColor).SetElementName("secondary_color");
            cm.MapProperty(u => u.GlowColorUnlocked).SetElementName("secondary_color_unlocked");
            cm.MapProperty(u => u.PokeyenBetRank).SetElementName("pokeyen_bet_rank");
            cm.MapProperty(u => u.IsSubscribed).SetElementName("subscriber");
            cm.MapProperty(u => u.MonthsSubscribed).SetElementName("months_subscribed")
                // 0 instead of null in code, but may be omitted in the database
                .SetDefaultValue(0)
                .SetIgnoreIfDefault(true);
            cm.MapProperty(u => u.SubscriptionTier).SetElementName("sub_plan");
            cm.MapProperty(u => u.LoyaltyLeague).SetElementName("loyalty_tier");
            cm.MapProperty(u => u.SubscriptionUpdatedAt).SetElementName("subscription_updated_at");
            cm.MapProperty(u => u.Roles).SetElementName("roles")
                .SetDefaultValue(new HashSet<Role>());
        });
    }

    public UserRepo(
        IMongoDatabase database,
        long startingPokeyen,
        long startingTokens,
        IImmutableList<string> defaultOperators,
        IClock clock)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<User>(CollectionName);
        _startingPokeyen = startingPokeyen;
        _startingTokens = startingTokens;
        _defaultOperators = defaultOperators.ToImmutableHashSet();
        _clock = clock;
        InitIndexes();

        foreach (string name in _defaultOperators)
        {
            User? user = FindBySimpleName(name.ToLower()).Result;
            if (user != null)
                SetRoles(user, new HashSet<Role> { Role.Operator });
        }
        // TODO currently pokeyen are not nullable in the user object, but in the current database some are.
        // There has been an unfinished discussion on whether nullable pokeyen are desired, e.g. to better represent
        // a balance reset, where each user with "null" would get the default amount on first load for example.
        // Until then, if the db runs into deserialization issues due to "null" pokeyen,
        // the following code can be used to automatically replace all nulls with zeroes at startup.
        // database.GetCollection<BsonDocument>(CollectionName).UpdateMany(
        // Builders<BsonDocument>.Filter.Eq("pokeyen", BsonNull.Value),
        // Builders<BsonDocument>.Update.Set("pokeyen", 0)
        // );
    }

    private void InitIndexes()
    {
        Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.SimpleName)),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.TwitchDisplayName)),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Pokeyen)),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Tokens)),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Roles)),
        });
    }

    public async Task<User> RecordUser(UserInfo userInfo)
    {
        Instant updatedAt = userInfo.UpdatedAt ?? _clock.GetCurrentInstant();
        UpdateDefinition<User> update = Builders<User>.Update
            .Set(u => u.TwitchDisplayName, userInfo.TwitchDisplayName)
            .Set(u => u.SimpleName, userInfo.SimpleName)
            .Set(u => u.LastActiveAt, updatedAt);
        if (userInfo.Color != null)
        {
            update = update.Set(u => u.Color, userInfo.Color.StringWithoutHash);
        }
        if (userInfo.FromMessage)
        {
            update = update.Set(u => u.LastMessageAt, updatedAt);
        }

        async Task<User?> UpdateExistingUser() => await Collection.FindOneAndUpdateAsync<User>(
            filter: u => u.Id == userInfo.Id,
            update: update,
            options: new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After, IsUpsert = false }
        );

        User? user = await UpdateExistingUser();
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
            color: userInfo.Color?.StringWithoutHash,
            firstActiveAt: updatedAt,
            lastActiveAt: updatedAt,
            lastMessageAt: userInfo.FromMessage ? updatedAt : null,
            pokeyen: _startingPokeyen,
            tokens: _startingTokens,
            roles: _defaultOperators.Contains(userInfo.SimpleName) ? new HashSet<Role> { Role.Operator } : null
        );
        try
        {
            await Collection.InsertOneAsync(document: user);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // oops, race condition!
            // Someone inserted the user after our check but before our insertion call just now.
            // Since it exists now, just re-attempt updating the existing entry.
            user = await UpdateExistingUser()
                   ?? throw new InvalidOperationException($"user {userInfo.SimpleName} must exist now!");
        }
        return user;
    }

    public async Task<User?> FindBySimpleName(string simpleName) =>
        await Collection.Find(u => u.SimpleName == simpleName).FirstOrDefaultAsync();

    public async Task<User?> FindByDisplayName(string displayName) =>
        await Collection.Find(u => u.TwitchDisplayName == displayName).FirstOrDefaultAsync();

    public async Task<List<User>> FindAllByPokeyenUnder(long yen) =>
        await Collection.Find(u => u.Pokeyen < yen).ToListAsync();

    public async Task<List<User>> FindAllByRole(Role role) =>
        await Collection.Find(u => u.Roles.Contains(role)).ToListAsync();

    private async Task<User> UpdateField<T>(User user, Expression<Func<User, T>> field, T value) =>
        await Collection.FindOneAndUpdateAsync<User>(
            filter: u => u.Id == user.Id,
            update: Builders<User>.Update.Set(field, value),
            options: new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After, IsUpsert = false })
        ?? throw new ArgumentException($"user {user} does not exist");

    public Task<User> SetSelectedBadge(User user, PkmnSpecies? badge) =>
        UpdateField(user, u => u.SelectedBadge, badge);

    public Task<User> SetSelectedEmblem(User user, int? emblem) =>
        UpdateField(user, u => u.SelectedParticipationEmblem, emblem);

    public Task<User> SetGlowColor(User user, string? glowColor) =>
        UpdateField(user, u => u.GlowColor, glowColor);

    public Task<User> SetGlowColorUnlocked(User user, bool unlocked) =>
        UpdateField(user, u => u.GlowColorUnlocked, unlocked);

    public Task<User> SetDisplayName(User user, string displayName) =>
        UpdateField(user, u => u.Name, displayName);

    public Task<User> SetRoles(User user, HashSet<Role> newRoles) =>
        UpdateField(user, u => u.Roles, newRoles);

    public async Task<bool> UnselectBadgeIfSpeciesSelected(string userId, PkmnSpecies species) =>
        await Collection.FindOneAndUpdateAsync<User>(
            filter: u => u.Id == userId && u.SelectedBadge == species,
            update: Builders<User>.Update.Set(u => u.SelectedBadge, null),
            options: new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After, IsUpsert = false })
        != null;

    public Task<User> SetIsSubscribed(User user, bool isSubscribed) =>
        UpdateField(user, u => u.IsSubscribed, isSubscribed);

    public async Task<User> SetSubscriptionInfo(User user,
        int monthsSubscribed, SubscriptionTier tier, int loyaltyLeague, Instant? subscriptionUpdatedAt)
        =>
            await Collection.FindOneAndUpdateAsync<User>(
                filter: u => u.Id == user.Id,
                update: Builders<User>.Update
                    .Set(u => u.MonthsSubscribed, monthsSubscribed)
                    .Set(u => u.SubscriptionTier, tier)
                    .Set(u => u.LoyaltyLeague, loyaltyLeague)
                    .Set(u => u.SubscriptionUpdatedAt, subscriptionUpdatedAt),
                options: new FindOneAndUpdateOptions<User>
                {
                    ReturnDocument = ReturnDocument.After,
                    IsUpsert = false
                })
            ?? throw new ArgumentException($"user {user} does not exist");
}
