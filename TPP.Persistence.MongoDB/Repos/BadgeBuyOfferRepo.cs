using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPP.Common;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Serializers;
using TPP.Persistence.Repos;

namespace TPP.Persistence.MongoDB.Repos
{
    public class BadgeBuyOfferRepo : IBadgeBuyOfferRepo
    {
        private const string CollectionName = "badgebuyoffers";

        public readonly IMongoCollection<BadgeBuyOffer> Collection;
        private readonly IBadgeRepo _badgeRepo;
        private readonly IClock _clock;

        static BadgeBuyOfferRepo()
        {
            BsonClassMap.RegisterClassMap<BadgeBuyOffer>(cm =>
            {
                cm.MapIdProperty(o => o.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(ObjectIdAsStringSerializer.Instance);
                cm.MapProperty(o => o.UserId).SetElementName("user");
                cm.MapProperty(o => o.Species).SetElementName("species");
                cm.MapProperty(o => o.Source).SetElementName("source");
                cm.MapProperty(o => o.CreatedAt).SetElementName("created_at");
                cm.MapProperty(o => o.Form).SetElementName("form")
                    .SetDefaultValue(0);
                cm.MapProperty(o => o.Shiny).SetElementName("shiny")
                    .SetDefaultValue(false)
                    .SetIgnoreIfDefault(true);
            });
        }

        public BadgeBuyOfferRepo(IMongoDatabase database, BadgeRepo badgeRepo, IClock clock)
        {
            _badgeRepo = badgeRepo;

            database.CreateCollectionIfNotExists(CollectionName).Wait();
            Collection = database.GetCollection<BadgeBuyOffer>(CollectionName);
            _clock = clock;
        }

        public async Task<BadgeBuyOffer> CreateBuyOffer(string userId, PkmnSpecies species, int? form, Badge.BadgeSource? source, bool? shiny, int price, int amount, Instant? createdAt=null)
        {
            BadgeBuyOffer buyOffer = new BadgeBuyOffer(
                id: string.Empty,
                userId: userId,
                species: species,
                form: form,
                source: source,
                shiny: shiny,
                price: price,
                amount: amount,
                createdAt: createdAt ?? _clock.GetCurrentInstant()
                );

            await Collection.InsertOneAsync(buyOffer);
            Debug.Assert(buyOffer.Id.Length > 0, "The MongoDB driver injected a generated ID");
            return buyOffer;
        }

    }
}
