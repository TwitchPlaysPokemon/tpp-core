using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using Persistence.Models;
using Persistence.MongoDB.Serializers;
using Persistence.Repos;

namespace Persistence.MongoDB.Repos
{
    public class BadgeRepo : IBadgeRepo
    {
        private const string CollectionName = "badges";

        public readonly IMongoCollection<Badge> Collection;

        static BadgeRepo()
        {
            BsonClassMap.RegisterClassMap<Badge>(cm =>
            {
                cm.MapIdProperty(b => b.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(ObjectIdAsStringSerializer.Instance);
                cm.MapProperty(b => b.UserId).SetElementName("user");
                cm.MapProperty(b => b.Species).SetElementName("species")
                    .SetSerializer(PkmnSpeciesSerializer.Instance);
                cm.MapProperty(b => b.Source).SetElementName("source")
                    .SetSerializer(BadgeSourceSerializer.Instance);
                cm.MapProperty(b => b.CreatedAt).SetElementName("created_at");
            });
        }

        public BadgeRepo(IMongoDatabase database)
        {
            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<Badge>(CollectionName);
            InitIndexes();
        }

        private void InitIndexes()
        {
            Collection.Indexes.CreateMany(new[]
            {
                new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.UserId)),
                new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.Species)),
                // TODO really ascending...?:
                new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.CreatedAt)),
            });
        }

        public async Task<Badge> AddBadge(string? userId, PkmnSpecies species, Badge.BadgeSource source)
        {
            var badge = new Badge(
                id: string.Empty,
                userId: userId,
                species: species,
                source: source,
                DateTime.UtcNow
            );
            await Collection.InsertOneAsync(badge);
            Debug.Assert(badge.Id.Length > 0, "The MongoDB driver injected a generated ID");
            return badge;
        }

        public async Task<List<Badge>> FindByUser(string? userId)
        {
            return await Collection.Find(b => b.UserId == userId).ToListAsync();
        }
    }
}
