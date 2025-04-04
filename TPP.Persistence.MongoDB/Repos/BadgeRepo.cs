using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NodaTime;
using TPP.Common;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;
using static MongoDB.Driver.ChangeStreamOperationType;

namespace TPP.Persistence.MongoDB.Repos;

public class BadgeRepo(
    IMongoDatabase database,
    ILogger<BadgeRepo> logger,
    IMongoBadgeLogRepo badgeLogRepo,
    IClock clock,
    Duration? rarityCalculationTransition = null)
    : IBadgeRepo, IBadgeStatsRepo, IAsyncInitRepo
{
    private const string CollectionName = "badges";
    private const string CollectionNameStats = "badgestats";

    public readonly IMongoCollection<Badge> Collection = database.GetCollection<Badge>(CollectionName);
    public readonly IMongoCollection<BadgeStat> CollectionStats = database.GetCollection<BadgeStat>(CollectionNameStats);

    private static readonly Instant WhenGen2WasAddedToPinball = Instant.FromUtc(2018, 3, 28, 12, 00);
    /// Instant at which some update was deployed that is expected to majorly disrupt badge rarities,
    /// e.g. if a whole new generation was added to pinball.
    private static readonly Instant LastRarityUpdate = WhenGen2WasAddedToPinball;

    /// Pinball drops enough badges that a somewhat small time-span is already statistically significant.
    /// Also, if Pinball ever gets tweaked and natural drop chances change, ignoring old badges gradually
    /// transitions to the new rarities.
    /// Added benefit: Recalculating a badge's rarity is faster on the DB.
    private readonly Duration _rarityCalculationTransition = rarityCalculationTransition ?? Duration.FromDays(365 * 3);

    /// Ratio at which badges from all sources are included in the rarity calculation,
    /// as opposed to only badges from "natural" generation sources (pinball).
    /// Because the rarity value is used to determine transmutation results, but transmuting directly changes
    /// how many of a species' badges exist, this is a relatively small percentage to dampen the feedback loop.
    public const double CountExistingFactor = 0.2;

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

    public async Task InitializeAsync()
    {
        await Collection.Indexes.CreateManyAsync([
            new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.UserId)),
            new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.Species)),
            new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.CreatedAt))
        ]);
        // we're watching a change stream and need to know the previous data on deletes and updates to recalc stats,
        // or more precisely to determine whether it's necessary to recalc stats
        await Collection.Database.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "collMod", CollectionName },
            { "changeStreamPreAndPostImages", new BsonDocument { { "enabled", true } } }
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
            createdAt: createdAt ?? clock.GetCurrentInstant()
        );
        await Collection.InsertOneAsync(badge);
        return badge;
    }

    public async Task<IImmutableList<Badge>> FindByUser(string? userId) =>
        (await Collection.Find(b => b.UserId == userId).ToListAsync()).ToImmutableList();

    public async Task<IImmutableList<Badge>> FindByUserAtTime(string userId, Instant timestamp)
    {
        IImmutableSet<string> badgeIds = await badgeLogRepo.FindBadgeIdsByUserAtTime(userId, timestamp);

        List<Badge> badges = await Collection.AsQueryable()
            .Where(badge => badgeIds.Contains(badge.Id))
            // Badge log may return IDs of badges that didn't exist at the given timestamp yet. Filter those out!
            .Where(badge => badge.CreatedAt <= timestamp)
            .ToListAsync();

        return badges.ToImmutableList();
    }

    public async Task<IImmutableList<string>> FindOwnerIdsForSpecies(PkmnSpecies species) =>
        (await Collection.AsQueryable()
            .Where(b => b.Species == species && b.UserId != null)
            .GroupBy(b => b.UserId! /* not null because of filter on not-null above */)
            .Select(grp => new { OwnerId = grp.Key, Count = grp.Count() })
            .OrderByDescending(grp => grp.Count)
            .Select(grp => grp.OwnerId)
            .ToListAsync()).ToImmutableList();

    public async Task<IImmutableList<Badge>> FindByUserAndSpecies(
        string? userId, PkmnSpecies species, int? limit = null)
    {
        var cursor = Collection.Find(b => b.UserId == userId && b.Species == species).Limit(limit);
        return (await cursor.ToListAsync()).ToImmutableList();
    }

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
        Instant now = clock.GetCurrentInstant();

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
                    await badgeLogRepo.LogWithSession(
                        badge.Id, reason, recipientUserId, badge.UserId, now, additionalData, txSession);
                return (object?)null;
            });
        }

        foreach (var tpl in badges.Select(b => (b.UserId, b.Species)).Distinct())
        {
            (string? previousOwnerUserId, PkmnSpecies species) = tpl;
            if (previousOwnerUserId != null && !await HasUserBadge(previousOwnerUserId, species))
                UserLostBadgeSpecies?.Invoke(this, new UserLostBadgeSpeciesEventArgs(previousOwnerUserId, species));
        }
        return updatedBadges.ToImmutableList();
    }

    private static IEnumerable<PkmnSpecies> GetRarityAffectingSpeciesFromChange(ChangeStreamDocument<Badge> c)
    {
        if (c is { OperationType: Insert, FullDocument: var inserted })
            yield return inserted.Species;
        if (c is { OperationType: Delete, FullDocumentBeforeChange: var deleted })
            yield return deleted.Species;
        if (c is { OperationType: Update or Replace, FullDocumentBeforeChange: var before, FullDocument: var after } &&
            ((before.UserId is null) != (after.UserId is null) || // consumed or consumption undone
             before.Species != after.Species || // changed species (shouldn't usually happen)
             before.CreatedAt != after.CreatedAt)) // creation date changed (shouldn't usually happen)
        {
            yield return before.Species;
            yield return after.Species;
        }
    }

    public async Task WatchAndHandleBadgeStatUpdates(CancellationToken cancellationToken)
    {
        IChangeStreamCursor<ChangeStreamDocument<Badge>> cursor = await Collection
            .WatchAsync(cancellationToken: cancellationToken, options: new ChangeStreamOptions
            {
                FullDocument = ChangeStreamFullDocumentOption.Required,
                FullDocumentBeforeChange = ChangeStreamFullDocumentBeforeChangeOption.Required
            });
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            ImmutableHashSet<PkmnSpecies> dirtySpecies = cursor.Current
                .SelectMany(GetRarityAffectingSpeciesFromChange)
                .ToImmutableHashSet();
            if (!dirtySpecies.IsEmpty)
                await RenewBadgeStats(dirtySpecies);
        }
    }

    public async Task RenewBadgeStats(IImmutableSet<PkmnSpecies>? onlyTheseSpecies = null)
    {
        var stopwatch = Stopwatch.StartNew();
        Instant now = clock.GetCurrentInstant();
        Instant startTime = Instant.Min(LastRarityUpdate, now - _rarityCalculationTransition);

        IAggregateFluent<Badge> pipeline = Collection.Aggregate(new AggregateOptions { AllowDiskUse = true });
        if (onlyTheseSpecies != null)
            pipeline = pipeline.Match(stat => onlyTheseSpecies.Contains(stat.Species));
        var stats = await pipeline
            .Group(b => b.Species, group => new
            {
                Species = group.Key,
                Count = group.Count(b => b.UserId != null),
                CountGenerated = group.Count(b => b.Source == Badge.BadgeSource.Pinball),
                RarityCount = group.Count(b => b.UserId != null && b.CreatedAt >= startTime),
                RarityCountGenerated =
                    group.Count(b => b.Source == Badge.BadgeSource.Pinball && b.CreatedAt >= startTime),
            })
            .SortBy(stat => stat.Species)
            .ToListAsync();

        long rarityTotalGenerated = await Collection.CountDocumentsAsync(b =>
            b.Source == Badge.BadgeSource.Pinball && b.CreatedAt >= startTime);
        long rarityTotalExisting = await Collection.CountDocumentsAsync(b =>
            b.UserId != null && b.CreatedAt >= startTime);

        if (stats.Count == 0) return;
        await CollectionStats.BulkWriteAsync(stats.Select(stat =>
            {
                double rarityGenerated = stat.RarityCountGenerated / (double)rarityTotalGenerated;
                double rarityExisting = stat.RarityCount / (double)rarityTotalExisting;
                double rarity = rarityGenerated * (1 - CountExistingFactor) + rarityExisting * CountExistingFactor;
                BadgeStat statEntity = new(
                    Species: stat.Species,
                    Count: stat.Count,
                    CountGenerated: stat.CountGenerated,
                    RarityCount: stat.RarityCount,
                    RarityCountGenerated: stat.RarityCountGenerated,
                    Rarity: rarity);
                return new ReplaceOneModel<BadgeStat>(
                    Builders<BadgeStat>.Filter.Where(b => b.Species == stat.Species),
                    statEntity)
                {
                    IsUpsert = true
                };
            }),
            new BulkWriteOptions { IsOrdered = false });
        if (onlyTheseSpecies != null)
            logger.LogDebug("Recalculated badge stats in {Elapsed}ms for these species: {Species}",
                stopwatch.ElapsedMilliseconds, string.Join(',', onlyTheseSpecies));
        else
            logger.LogDebug("Recalculated badge stats in {Elapsed}ms for all species", stopwatch.ElapsedMilliseconds);
    }

    public async Task<ImmutableSortedDictionary<PkmnSpecies, BadgeStat>> GetBadgeStats()
    {
        List<BadgeStat> badgeStats = await CollectionStats
            .Find(FilterDefinition<BadgeStat>.Empty)
            .ToListAsync();
        return badgeStats.ToImmutableSortedDictionary(stat => stat.Species, stat => stat);
    }

    public async Task<BadgeStat?> GetBadgeStatsForSpecies(PkmnSpecies species)
    {
        return await CollectionStats
            .Find(stat => stat.Species == species)
            .SingleOrDefaultAsync();
    }
}
