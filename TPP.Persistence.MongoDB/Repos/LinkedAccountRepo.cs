using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos;

internal record LinkedAccount(string Id, HashSet<string> UserIds);

public class LinkedAccountRepo : ILinkedAccountRepo
{
    private const string CollectionName = "linked_accounts";
    private const string UserIdsMemberName = "user_ids";

    private readonly IMongoCollection<LinkedAccount> _collection;
    private readonly IMongoCollection<User> _userCollection;

    static LinkedAccountRepo()
    {
        BsonClassMap.RegisterClassMap<LinkedAccount>(cm =>
        {
            cm.MapIdProperty(b => b.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(b => b.UserIds).SetElementName(UserIdsMemberName);
        });
    }

    public LinkedAccountRepo(IMongoDatabase database, IMongoCollection<User> userCollection)
    {
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        _collection = database.GetCollection<LinkedAccount>(CollectionName);
        _userCollection = userCollection;
    }

    public async Task<IImmutableSet<User>> FindLinkedUsers(string userId) =>
        (await _collection.Aggregate()
            .Match(links => links.UserIds.Contains(userId))
            .Unwind(l => l.UserIds)
            .Lookup<BsonDocument, User, BsonDocument>(
                foreignCollection: _userCollection,
                localField: bson => bson[UserIdsMemberName],
                foreignField: user => user.Id,
                @as: bson => bson["user_obj"])
            .ReplaceWith(bson => bson["user_obj"][0])
            .As<User>()
            .ToListAsync()).ToImmutableHashSet();

    public async Task<bool> Link(IImmutableSet<string> userIds)
    {
        List<LinkedAccount> linkedAccountEntries = await _collection
            // .Find(l => l.UserIds.Intersect(userIds).Any())
            .Find(Builders<LinkedAccount>.Filter.AnyIn(l => l.UserIds, userIds))
            .ToListAsync();
        ImmutableHashSet<string> existingUserIds = linkedAccountEntries
            .SelectMany(l => l.UserIds)
            .ToImmutableHashSet();
        ImmutableHashSet<string> allUserIds = existingUserIds.Union(userIds);
        if (existingUserIds.Count == allUserIds.Count)
            return false; // already marked as linked

        // don't bother patching existing documents, just delete all old ones and make a new one
        await _collection.InsertOneAsync(new LinkedAccount(string.Empty, allUserIds.ToHashSet()));
        foreach ((string id, _) in linkedAccountEntries)
            await _collection.DeleteOneAsync(l => l.Id == id);
        return true;
    }

    public async Task<bool> Unlink(string userId)
    {
        UpdateResult updateResult = await _collection.UpdateManyAsync(
            l => l.UserIds.Contains(userId),
            Builders<LinkedAccount>.Update.Pull(l => l.UserIds, userId));
        await _collection.DeleteManyAsync(l => l.UserIds.Count < 2);
        return updateResult.ModifiedCount > 0;
    }
}
