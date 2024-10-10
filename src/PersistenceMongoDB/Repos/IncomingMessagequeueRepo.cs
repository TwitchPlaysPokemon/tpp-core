using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using Model;
using PersistenceMongoDB.Serializers;

namespace PersistenceMongoDB.Repos;

public class IncomingMessagequeueRepo : IIncomingMessagequeueRepo
{
    public const string CollectionName = "messagequeue_in";

    public readonly IMongoCollection<IncomingMessagequeueItem> Collection;

    private readonly ILogger<IncomingMessagequeueRepo> _logger;

    static IncomingMessagequeueRepo()
    {
        BsonClassMap.RegisterClassMap<IncomingMessagequeueItem>(cm =>
        {
            cm.MapIdProperty(i => i.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(ObjectIdAsStringSerializer.Instance);
            cm.MapProperty(i => i.Message).SetElementName("message");
            cm.MapProperty(i => i.MessageType).SetElementName("type");
            cm.MapProperty(i => i.Target).SetElementName("target");
            cm.MapProperty(i => i.QueuedAt).SetElementName("queued_at")
                .SetDefaultValue(Instant.MinValue);
        });
    }

    public IncomingMessagequeueRepo(IMongoDatabase database, ILogger<IncomingMessagequeueRepo> logger)
    {
        _logger = logger;
        database.CreateCollectionIfNotExists(CollectionName).Wait();
        Collection = database.GetCollection<IncomingMessagequeueItem>(CollectionName);
    }

    public async Task Prune(Instant olderThan)
    {
        await Collection.DeleteManyAsync(item => item.QueuedAt < olderThan);
    }

    public async Task ForEachAsync(
        Func<IncomingMessagequeueItem, Task> process,
        CancellationToken cancellationToken)
    {
        IChangeStreamCursor<ChangeStreamDocument<IncomingMessagequeueItem>> cursor =
            await Collection.WatchAsync(cancellationToken: cancellationToken);

        await cursor.ForEachAsync(async change =>
        {
            if (change.OperationType == ChangeStreamOperationType.Insert)
            {
                IncomingMessagequeueItem doc = change.FullDocument;
                await Collection.DeleteOneAsync(u => u.Id == doc.Id, cancellationToken);
                await process(doc);
            }
            else if (change.OperationType == ChangeStreamOperationType.Delete)
            {
                // We immediately delete all documents we process, so we can safely ignore all delete events
            }
            else
            {
                _logger.LogError(
                    "Unexpected change stream event of type '{EventType}' on collection {Collection}: {Event}",
                    change.OperationDescription, CollectionName, change.ToString());
            }
        }, cancellationToken);
    }
}
