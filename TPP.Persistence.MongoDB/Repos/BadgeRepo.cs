using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        private readonly IMongoBadgeLogRepo _badgeLogRepo;
        private readonly IClock _clock;

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
                cm.MapProperty(b => b.Form).SetElementName("form");
                cm.MapProperty(b => b.SellPrice).SetElementName("sell_price")
                    .SetIgnoreIfNull(true);
                cm.MapProperty(b => b.SellingSince).SetElementName("selling_since")
                    .SetIgnoreIfNull(true);
                cm.MapProperty(b => b.Shiny).SetElementName("shiny")
                    .SetDefaultValue(false)
                    .SetIgnoreIfDefault(true);
            });
        }

        public BadgeRepo(IMongoDatabase database, IMongoBadgeLogRepo badgeLogRepo, IClock clock)
        {
            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<Badge>(CollectionName);
            _badgeLogRepo = badgeLogRepo;
            _clock = clock;
            InitIndexes();
        }

        private void InitIndexes()
        {
            Collection.Indexes.CreateMany(new[]
            {
                new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.UserId)),
                new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.Species)),
                new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.Source)),
                // TODO really ascending...?:
                new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.CreatedAt)),
                new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.Form)),
                new CreateIndexModel<Badge>(Builders<Badge>.IndexKeys.Ascending(u => u.Shiny)),
            });
        }

        public async Task<Badge> AddBadge(
            string? userId, PkmnSpecies species, Badge.BadgeSource source, string? form, bool shiny, Instant? createdAt = null)
        {
            var badge = new Badge(
                id: string.Empty,
                userId: userId,
                species: species,
                source: source,
                createdAt: createdAt ?? _clock.GetCurrentInstant(),
                form: form,
                shiny: shiny
            );
            await Collection.InsertOneAsync(badge);
            Debug.Assert(badge.Id.Length > 0, "The MongoDB driver injected a generated ID");
            return badge;
        }

        public async Task<List<Badge>> FindAllByUser(string? userId) =>
            await Collection.Find(b => b.UserId == userId).ToListAsync();

        public async Task<List<Badge>> FindByUserAndSpecies(string? userId, PkmnSpecies species) =>
            await Collection.Find(b => b.UserId == userId && b.Species == species).ToListAsync();

        public async Task<List<Badge>> FindAllByCustom(string? userId, PkmnSpecies? species, string? form, Badge.BadgeSource? source, bool? shiny)
        {
            FilterDefinition<Badge> filter = Builders<Badge>.Filter.Empty;
            if (userId != null)
                filter &= Builders<Badge>.Filter.Eq(b => b.UserId, userId);
            else
                filter &= Builders<Badge>.Filter.Ne(b => b.UserId, null);
            if (species != null)
                filter &= Builders<Badge>.Filter.Eq(b => b.Species, species);
            if (form != null)
                filter &= Builders<Badge>.Filter.Eq(b => b.Form, form);
            if (source != null)
                filter &= Builders<Badge>.Filter.Eq(b => b.Source, source);
            if (shiny == true)
                filter &= Builders<Badge>.Filter.Eq(b => b.Shiny, true);
            else
                //match everything not true, including null/nonexistance. (querying for shiny==false misses these)
                filter &= Builders<Badge>.Filter.Ne(b => b.Shiny, true);

            return await Collection.Find(filter).ToListAsync();
        }

        public async Task<List<Badge>> FindAllForSaleByCustom(string? userId, PkmnSpecies? species, string? form, Badge.BadgeSource? source, bool? shiny)
        {
            List<Badge> all = await FindAllByCustom(userId, species, form, source, shiny);
            return all.Where(b => b.SellPrice > 0).ToList();
        }

        public async Task<List<Badge>> FindAllNotForSaleByCustom(string? userId, PkmnSpecies? species, string? form, Badge.BadgeSource? source, bool? shiny)
        {
            List<Badge> all = await FindAllByCustom(userId, species, form, source, shiny);
            return all.Where(b => b.SellPrice == 0).ToList();
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

            foreach (var tpl in badges.Select(b => (b.UserId, b.Species)).Distinct())
            {
                (string? previousOwnerUserId, PkmnSpecies species) = tpl;
                if (previousOwnerUserId != null && !await HasUserBadge(previousOwnerUserId, species))
                    UserLostBadgeSpecies?.Invoke(this, new UserLostBadgeSpeciesEventArgs(previousOwnerUserId, species));
            }
            return updatedBadges.ToImmutableList();
        }
        public async Task<Badge> SetBadgeSellPrice(Badge badge, long price)
        {
            if (price <= 0)
                throw new ArgumentOutOfRangeException("price", "Price must be positive");
            return await Collection.FindOneAndUpdateAsync<Badge>(
                Builders<Badge>.Filter
                           .Where(b => b.Id == badge.Id && b.UserId == badge.UserId),
                Builders<Badge>.Update
                    .Set(b => b.SellPrice, price)
                    .Set(b => b.SellingSince, _clock.GetCurrentInstant()),
                new FindOneAndUpdateOptions<Badge> { ReturnDocument = ReturnDocument.After, IsUpsert = false }
                ) ?? throw new OwnedBadgeNotFoundException(badge);
        }
    }
}
