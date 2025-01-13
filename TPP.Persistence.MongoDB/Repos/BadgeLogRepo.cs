using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;

namespace TPP.Persistence.MongoDB.Repos;

/// <summary>
/// MongoDB-specific extension of <see cref="IBadgeLogRepo"/> which is required because <see cref="BadgeRepo"/>
/// needs to additionally pass <see cref="IClientSessionHandle"/>s to be able to log within a transaction.
/// </summary>
public interface IMongoBadgeLogRepo : IBadgeLogRepo
{
    Task<BadgeLog> LogWithSession(
        string badgeId, string badgeLogType, string? userId, string? oldUserId, Instant timestamp,
        IDictionary<string, object?>? additionalData = null,
        IClientSessionHandle? session = null);
}

public class BadgeLogRepo(IMongoDatabase database, ILogger<BadgeLogRepo> logger) : IMongoBadgeLogRepo, IAsyncInitRepo
{
    public const string CollectionName = "badgelog";
    /// Any badge logs before this date (commit 0ff8dd7f in old core) didn't track the "old user" field,
    /// so they cannot reliably be used to track ownership of badges.
    /// Retroactively fixing this information is complicated, as the best thing I could think of is manually
    /// correlating the badge logs with command logs.
    public static readonly Instant CorrectOwnershipTrackingSince = Instant.FromUtc(2016, 12, 4, 0, 0);

    public readonly IMongoCollection<BadgeLog> Collection = database.GetCollection<BadgeLog>(CollectionName);

    public async Task InitializeAsync()
    {
        await Collection.Indexes.CreateManyAsync([
            new CreateIndexModel<BadgeLog>(Builders<BadgeLog>.IndexKeys.Ascending(u => u.UserId)),
            new CreateIndexModel<BadgeLog>(Builders<BadgeLog>.IndexKeys.Ascending(u => u.OldUserId)),
            new CreateIndexModel<BadgeLog>(Builders<BadgeLog>.IndexKeys.Ascending(u => u.Timestamp)),
            new CreateIndexModel<BadgeLog>(Builders<BadgeLog>.IndexKeys.Ascending(u => u.BadgeId)),
        ]);

        Task _ = Task.Run(async () =>
        {
            // Fixing the logs for "purchase", "gift" and "gift_remote" is easier done manually with these queries:
            // db.getCollection('badgelog').updateMany({event: "gift", old_user: null}, [{$set: {old_user: "$gifter"}}])
            // db.getCollection('badgelog').updateMany({event: "gift_remote", old_user: null}, [{$set: {old_user: "$gifter"}}])
            // db.getCollection('badgelog').updateMany({event: "purchase", old_user: null}, [{$set: {old_user: "$seller"}}])

            // For transmutations, I couldn't figure out a query, so it's done in code:
            // Look up the transmutation log closest to the badge log, and get the user from there.
            // Don't try to repair anything before transmutations actually logged anything.
            List<BadgeLog> corrupted = await Collection
                .Find(log => log.BadgeLogType == BadgeLogType.Transmutation
                             && log.OldUserId == null
                             && log.Timestamp >= CorrectOwnershipTrackingSince)
                .ToListAsync();
            if (corrupted.Count == 0)
                return;
            logger.LogWarning("Found {Num} transmutation badge logs without 'old_user' field, trying to repair... " +
                              "this warning only appears at boot and should eventually vanish", corrupted.Count);
            var transmutationLogs = database.GetCollection<TransmutationLog>(TransmutationLogRepo.CollectionName);
            int counter = 0;
            foreach (BadgeLog badgeLog in corrupted)
            {
                List<TransmutationLog> lol = await transmutationLogs
                    .Find(tl => tl.InputBadges.Contains(badgeLog.BadgeId))
                    .ToListAsync();
                TransmutationLog? transmutationLog = lol
                    .OrderBy(tl => Math.Abs((tl.Timestamp - badgeLog.Timestamp).TotalMilliseconds))
                    .FirstOrDefault();
                string userId;
                if (transmutationLog == null)
                {
                    if (badgeLog.Id is "6504cc48b85ef5f2c2c54fe3" or "6504cc48b85ef5f2c2c54fe4" or "6504cc48b85ef5f2c2c54fe5")
                    {
                        // For these the transmutation is missing bc of a MongoWriteConcernException at 2023-09-15.
                        // See https://discord.com/channels/333356453928894466/579758730418192399/1152355105102905424
                        userId = "627143068";
                    }
                    else
                    {
                        logger.LogError("Could not find transmutation log for badge log {BadgeLog}", badgeLog);
                        continue;
                    }
                }
                else
                {
                    userId = transmutationLog.UserId;
                }
                await Collection.UpdateOneAsync(log => log.Id == badgeLog.Id,
                    Builders<BadgeLog>.Update.Set(l => l.OldUserId, userId));
                counter++;
                if (counter % 100 == 0)
                    logger.LogInformation("Repaired {Counter}/{Total} transmute badge logs", counter, corrupted.Count);
            }
        });
    }

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
            cm.MapProperty(b => b.OldUserId).SetElementName("old_user");
            cm.MapProperty(b => b.Timestamp).SetElementName("ts");
            cm.MapExtraElementsProperty(b => b.AdditionalData);
        });
    }

    public async Task<BadgeLog> LogWithSession(
        string badgeId, string badgeLogType, string? userId, string? oldUserId, Instant timestamp,
        IDictionary<string, object?>? additionalData = null,
        IClientSessionHandle? session = null)
    {
        var item = new BadgeLog(string.Empty, badgeId, badgeLogType, userId, oldUserId, timestamp,
            additionalData ?? ImmutableDictionary<string, object?>.Empty);
        if (session != null)
            await Collection.InsertOneAsync(session, item);
        else
            await Collection.InsertOneAsync(item);
        return item;
    }

    public Task<BadgeLog> Log(string badgeId, string badgeLogType, string? userId, string? oldUserId, Instant timestamp,
        IDictionary<string, object?>? additionalData = null) =>
        LogWithSession(badgeId, badgeLogType, userId, oldUserId, timestamp, additionalData, session: null);
}
