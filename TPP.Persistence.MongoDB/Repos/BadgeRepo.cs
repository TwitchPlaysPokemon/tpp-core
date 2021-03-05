using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
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
    public class BadgeRepo : IBadgeRepo, IBadgeStatsRepo
    {
        private const string CollectionName = "badges";
        private const string CollectionNameStats = "badgestats";

        public readonly IMongoCollection<Badge> Collection;
        public readonly IMongoCollection<BadgeStat> CollectionStats;
        private readonly IMongoBadgeLogRepo _badgeLogRepo;
        private readonly IClock _clock;

        /// Instant at which some update was deployed that is expected to majorly disrupt badge rarities,
        /// e.g. if a whole new generation was added to pinball.
        private readonly Instant _lastRarityUpdate;

        /// Minimum amount of time that must pass before a badge that was created before the last rarity update
        /// gets ignored for rarity calculations. This is necessary to have a smooth rarity transition period.
        private readonly Duration _rarityCalculationTransition;

        private readonly Instant _whenGen2WasAddedToPinball = Instant.FromUtc(2018, 3, 28, 12, 00);

        /// Ratio at which badges from all sources are included in the rarity calculation,
        /// as opposed to only badges from "natural" generation sources (pinball).
        /// Because the rarity value is used to determine transmutation results, but transmuting directly changes
        /// how many of a species' badges exist, this is a relatively small percentage to dampen the feedback loop.
        private const double CountExistingFactor = 0.2;

        static BadgeRepo()
        {
            Debug.Assert(CountExistingFactor >= 0 && CountExistingFactor <= 1, "factor must be a ratio");
            BsonClassMap.RegisterClassMap<Badge>(cm =>
            {
                cm.MapIdProperty(b => b.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(ObjectIdAsStringSerializer.Instance);
                cm.MapProperty(b => b.UserId).SetElementName("user");
                cm.MapProperty(b => b.Species).SetElementName("species");
                cm.MapProperty(b => b.Source).SetElementName("source");
                cm.MapProperty(b => b.CreatedAt).SetElementName("created_at");
                cm.MapProperty(b => b.SellPrice).SetElementName("sell_price")
                    .SetIgnoreIfNull(true);
                cm.MapProperty(b => b.SellingSince).SetElementName("selling_since")
                    .SetIgnoreIfNull(true);
            });
            BsonClassMap.RegisterClassMap<BadgeStat>(cm =>
            {
                cm.MapIdProperty(b => b.Species)
                    .SetSerializer(PkmnSpeciesSerializer.Instance);
                cm.MapProperty(b => b.Count).SetElementName("count");
                cm.MapProperty(b => b.CountGenerated).SetElementName("count_generated");
                cm.MapProperty(b => b.RarityCount).SetElementName("rarity_count");
                cm.MapProperty(b => b.RarityCountGenerated).SetElementName("rarity_count_generated");
                cm.MapProperty(b => b.Rarity).SetElementName("rarity");
            });
        }

        public BadgeRepo(
            IMongoDatabase database,
            IMongoBadgeLogRepo badgeLogRepo,
            IClock clock,
            Instant? lastRarityUpdate = null,
            Duration? rarityCalculationTransition = null)
        {
            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<Badge>(CollectionName);
            CollectionStats = database.GetCollection<BadgeStat>(CollectionNameStats);
            _badgeLogRepo = badgeLogRepo;
            _clock = clock;
            _lastRarityUpdate = lastRarityUpdate ?? _whenGen2WasAddedToPinball;
            _rarityCalculationTransition = rarityCalculationTransition ?? Duration.FromDays(90);
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

        public async Task<Badge> AddBadge(
            string? userId, PkmnSpecies species, Badge.BadgeSource source, Instant? createdAt = null)
        {
            var badge = new Badge(
                id: string.Empty,
                userId: userId,
                species: species,
                source: source,
                createdAt: createdAt ?? _clock.GetCurrentInstant()
            );
            await Collection.InsertOneAsync(badge);
            Debug.Assert(badge.Id.Length > 0, "The MongoDB driver injected a generated ID");
            return badge;
        }

        public async Task<List<Badge>> FindByUser(string? userId) =>
            await Collection.Find(b => b.UserId == userId).ToListAsync();

        public async Task<List<Badge>> FindByUserAndSpecies(string? userId, PkmnSpecies species) =>
            await Collection.Find(b => b.UserId == userId && b.Species == species).ToListAsync();

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

        public event EventHandler<UserLostBadgeSpeciesEventArgs>? UserLostBadgeSpecies;

        private async Task<Badge> TransferBadge(
            Badge badge, string? recipientUserId, IClientSessionHandle session, CancellationToken cancellationToken)
        {
            if (badge.UserId == recipientUserId)
                throw new ArgumentException($"badge {badge} is already owned by user with id {recipientUserId}");
            return await Collection.FindOneAndUpdateAsync(session,
                       Builders<Badge>.Filter
                           .Where(b => b.Id == badge.Id && b.UserId == badge.UserId),
                       Builders<Badge>.Update
                           .Set(b => b.UserId, recipientUserId)
                           .Unset(b => b.SellPrice)
                           .Unset(b => b.SellingSince),
                       new FindOneAndUpdateOptions<Badge> { ReturnDocument = ReturnDocument.After, IsUpsert = false },
                       cancellationToken)
                   ?? throw new OwnedBadgeNotFoundException(badge);
        }

        public async Task<IImmutableList<Badge>> TransferBadges(
            IImmutableList<Badge> badges, string? recipientUserId, string reason,
            IDictionary<string, object?> additionalData)
        {
            Instant now = _clock.GetCurrentInstant();

            List<Badge> updatedBadges = new();
            using (IClientSessionHandle sessionOuter = await Collection.Database.Client.StartSessionAsync())
            {
                await sessionOuter.WithTransactionAsync(async (txSession, txToken) =>
                {
                    foreach (Badge badge in badges)
                    {
                        Badge updatedBadge = await TransferBadge(badge, recipientUserId, txSession, txToken);
                        Debug.Assert(badge.Id == updatedBadge.Id);
                        updatedBadges.Add(updatedBadge);
                    }

                    foreach (Badge badge in badges)
                        await _badgeLogRepo.LogWithSession(
                            badge.Id, reason, recipientUserId, now, additionalData, txSession);
                    return (object?)null;
                });
            }
            await RenewBadgeStats(onlyTheseSpecies: badges.Select(b => b.Species).ToImmutableHashSet());

            foreach (var tpl in badges.Select(b => (b.UserId, b.Species)).Distinct())
            {
                (string? previousOwnerUserId, PkmnSpecies species) = tpl;
                if (previousOwnerUserId != null && !await HasUserBadge(previousOwnerUserId, species))
                    UserLostBadgeSpecies?.Invoke(this, new UserLostBadgeSpeciesEventArgs(previousOwnerUserId, species));
            }
            return updatedBadges.ToImmutableList();
        }

        public async Task RenewBadgeStats(IImmutableSet<PkmnSpecies>? onlyTheseSpecies = null)
        {
            Instant now = _clock.GetCurrentInstant();
            Instant startTime = Instant.Min(_lastRarityUpdate, now - _rarityCalculationTransition);

            IAggregateFluent<Badge> pipeline = Collection.Aggregate();
            if (onlyTheseSpecies != null)
                pipeline = pipeline.Match(stat => onlyTheseSpecies.Contains(stat.Species));
            // TODO workaround for https://jira.mongodb.org/browse/CSHARP-3449
            // Using LINQ causes the badge source enum value to be implicitly converted to its ordinal value,
            // which causes the comparisons against the "source" field to never match and give wrong results.
            // Remove this JSON blob and uncomment the code below once there is a solution for that.
            List<BsonDocument> stats = await pipeline.Group(@"
            {
                ""_id"": ""$species"",
                ""count"": {""$sum"": {
                    ""$cond"": [{""$ne"": [""$user"", null]}, 1, 0]
                }},
                ""count_generated"": {""$sum"": {
                    ""$cond"": [{""$eq"": [""$source"", ""pinball""]}, 1, 0]
                }},
                ""rarity_count"": {""$sum"": {
                    ""$cond"": [{""$and"": [{""$ne"": [""$user"", null]},
                                            {""$gte"": [""$created_at"", ISODate(""" + startTime + @""")]}]}, 1, 0]
                }},
                ""rarity_count_generated"": {""$sum"": {
                    ""$cond"": [{""$and"": [{""$eq"": [""$source"", ""pinball""]},
                                            {""$gte"": [""$created_at"", ISODate(""" + startTime + @""")]}]}, 1, 0]
                }}
            }").ToListAsync();
            // var stats = await pipeline
            //     .Group(b => b.Species, group => new
            //     {
            //         Species = group.Key,
            //         Count = group.Count(b => b.UserId != null),
            //         CountGenerated = group.Count(b => b.Source == Badge.BadgeSource.ManualCreation),
            //         RarityCount = group.Count(b => b.UserId != null && b.CreatedAt >= startTime),
            //         RarityCountGenerated =
            //             group.Count(b => b.Source == Badge.BadgeSource.ManualDistribution && b.CreatedAt >= startTime),
            //     })
            //     .SortBy(stat => stat.Species)
            //     .ToListAsync();

            long totalGenerated = await Collection.CountDocumentsAsync(b =>
                b.Source == Badge.BadgeSource.Pinball && b.CreatedAt >= startTime);
            long totalExisting = await Collection.CountDocumentsAsync(b =>
                b.UserId != null && b.CreatedAt >= startTime);

            if (stats.Count == 0) return;
            await CollectionStats.BulkWriteAsync(stats.Select(stat =>
                {
                    double rarityGenerated = stat["rarity_count_generated"].AsInt32 /*stat.RarityCountGenerated*/ /
                                             (double)totalGenerated;
                    double rarityExisting = stat["rarity_count"].AsInt32 /*stat.RarityCount*/ / (double)totalExisting;
                    double rarity = rarityGenerated * (1 - CountExistingFactor) + rarityExisting * CountExistingFactor;
                    BadgeStat statEntity = new(
                        Species: PkmnSpecies.OfId(stat["_id"].AsString) /*stat.Species*/,
                        Count: stat["count"].AsInt32 /*stat.Count*/,
                        CountGenerated: stat["count_generated"].AsInt32 /*stat.CountGenerated*/,
                        RarityCount: stat["rarity_count"].AsInt32 /*stat.RarityCount*/,
                        RarityCountGenerated: stat["rarity_count_generated"].AsInt32 /*stat.RarityCountGenerated*/,
                        Rarity: rarity);
                    return new ReplaceOneModel<BadgeStat>(
                        Builders<BadgeStat>.Filter.Where(b =>
                            b.Species == PkmnSpecies.OfId(stat["_id"].AsString) /*stat.Species*/),
                        statEntity)
                    {
                        IsUpsert = true
                    };
                }),
                new BulkWriteOptions { IsOrdered = false });
        }

        public async Task<ImmutableSortedDictionary<PkmnSpecies, BadgeStat>> GetBadgeStats() =>
            (await CollectionStats
                .Find(FilterDefinition<BadgeStat>.Empty)
                .ToListAsync())
            .ToImmutableSortedDictionary(stat => stat.Species, stat => stat);
    }
}
