using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NodaTime;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Serializers;
using TPP.Persistence.Repos;

namespace TPP.Persistence.MongoDB.Repos
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
                cm.MapProperty(b => b.Species).SetElementName("species");
                cm.MapProperty(b => b.Source).SetElementName("source");
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
                createdAt: Instant.FromUnixTimeSeconds(0)
            );
            await Collection.InsertOneAsync(badge);
            Debug.Assert(badge.Id.Length > 0, "The MongoDB driver injected a generated ID");
            return badge;
        }

        public async Task<List<Badge>> FindByUser(string? userId) =>
            await Collection.Find(b => b.UserId == userId).ToListAsync();

        public async Task<long> CountByUserAndSpecies(string? userId, PkmnSpecies species) =>
            await Collection.CountDocumentsAsync(b => b.UserId == userId && b.Species == species);

        public async Task<ImmutableSortedDictionary<PkmnSpecies, int>> CountByUserPerSpecies(string? userId)
        {
            var query =
                from badge in Collection.AsQueryable()
                where badge.UserId == userId
                group badge by badge.Species
                into gr
                orderby gr.Key
                select new { Id = gr.Key, Count = gr.Count() };
            var numBadgesPerSpecies = await query.ToListAsync();
            return numBadgesPerSpecies.ToImmutableSortedDictionary(kvp => kvp.Id, kvp => kvp.Count);
        }

        public async Task<bool> HasUserBadge(string? userId, PkmnSpecies species) =>
            await Collection
                .Find(b => b.Species == species && b.UserId == userId)
                .AnyAsync();
    }
}
