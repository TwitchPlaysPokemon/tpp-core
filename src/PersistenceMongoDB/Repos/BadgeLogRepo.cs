using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos
{
    /// <summary>
    /// MongoDB-specific extension of <see cref="IBadgeLogRepo"/> which is required because <see cref="BadgeRepo"/>
    /// needs to additionally pass <see cref="IClientSessionHandle"/>s to be able to log within a transaction.
    /// </summary>
    public interface IMongoBadgeLogRepo : IBadgeLogRepo
    {
        Task<BadgeLog> LogWithSession(
            string badgeId, string badgeLogType, string? userId, Instant timestamp,
            IDictionary<string, object?>? additionalData = null,
            IClientSessionHandle? session = null);
    }

    public class BadgeLogRepo : IMongoBadgeLogRepo
    {
        public const string CollectionName = "badgelog";

        public readonly IMongoCollection<BadgeLog> Collection;

        static BadgeLogRepo()
        {
            BsonClassMap.RegisterClassMap<BadgeLog>(cm =>
            {
                cm.MapIdProperty(b => b.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(ObjectIdAsStringSerializer.Instance);
                cm.MapProperty(b => b.BadgeId).SetElementName("badge")
                    .SetSerializer(ObjectIdAsStringSerializer.Instance);
                cm.MapProperty(b => b.BadgeLogType).SetElementName("event");
                cm.MapProperty(b => b.UserId).SetElementName("user");
                cm.MapProperty(b => b.Timestamp).SetElementName("ts");
                cm.MapExtraElementsProperty(b => b.AdditionalData);
            });
        }

        public BadgeLogRepo(IMongoDatabase database)
        {
            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<BadgeLog>(CollectionName);
        }

        public async Task<BadgeLog> LogWithSession(
            string badgeId, string badgeLogType, string? userId, Instant timestamp,
            IDictionary<string, object?>? additionalData = null,
            IClientSessionHandle? session = null)
        {
            var item = new BadgeLog(string.Empty, badgeId, badgeLogType, userId, timestamp,
                additionalData ?? ImmutableDictionary<string, object?>.Empty);
            if (session != null)
                await Collection.InsertOneAsync(session, item);
            else
                await Collection.InsertOneAsync(item);
            return item;
        }

        public Task<BadgeLog> Log(string badgeId, string badgeLogType, string? userId, Instant timestamp,
            IDictionary<string, object?>? additionalData = null) =>
            LogWithSession(badgeId, badgeLogType, userId, timestamp, additionalData, session: null);
    }
}
