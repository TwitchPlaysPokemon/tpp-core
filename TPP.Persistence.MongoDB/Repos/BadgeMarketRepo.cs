using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;
using TPP.Common;
using TPP.Model;
using TPP.Persistence.MongoDB.Serializers;
using TPP.Persistence.Repos;

namespace TPP.Persistence.MongoDB.Repos
{
    public class BadgeMarketRepo : IBadgeMarketRepo
    {
        private const string BuyOfferCollectionName = "badgebuyoffers";
        public readonly IMongoCollection<BadgeBuyOffer> BuyOfferCollection;
        private readonly IBadgeRepo _badgeRepo;
        private readonly IClock _clock;
        private readonly IUserRepo _userRepo;
        private readonly IBank<User> _tokenBank;

        static BadgeMarketRepo()
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
                cm.MapProperty(o => o.Price).SetElementName("price");
                cm.MapProperty(o => o.Amount).SetElementName("amount");
                cm.MapProperty(o => o.Form).SetElementName("form")
                   .SetIgnoreIfNull(true);
                cm.MapProperty(o => o.Shiny).SetElementName("shiny")
                    .SetDefaultValue(false)
                    .SetIgnoreIfDefault(true);
            });
        }

        private void InitIndexes()
        {
            BuyOfferCollection.Indexes.CreateMany(new[]
            {
                new CreateIndexModel<BadgeBuyOffer>(Builders<BadgeBuyOffer>.IndexKeys.Ascending(u => u.UserId)),
                new CreateIndexModel<BadgeBuyOffer>(Builders<BadgeBuyOffer>.IndexKeys.Ascending(u => u.Species)),
                new CreateIndexModel<BadgeBuyOffer>(Builders<BadgeBuyOffer>.IndexKeys.Ascending(u => u.Source)),
                new CreateIndexModel<BadgeBuyOffer>(Builders<BadgeBuyOffer>.IndexKeys.Ascending(u => u.CreatedAt)),
                new CreateIndexModel<BadgeBuyOffer>(Builders<BadgeBuyOffer>.IndexKeys.Ascending(u => u.Form)),
                new CreateIndexModel<BadgeBuyOffer>(Builders<BadgeBuyOffer>.IndexKeys.Ascending(u => u.Shiny)),
            });
        }

        public BadgeMarketRepo(IMongoDatabase database, IBadgeRepo badgeRepo, IUserRepo userRepo, IBank<User> bank, IClock clock)
        {
            _badgeRepo = badgeRepo;
            _userRepo = userRepo;
            _tokenBank = bank;
            database.CreateCollectionIfNotExists(BuyOfferCollectionName).Wait();
            BuyOfferCollection = database.GetCollection<BadgeBuyOffer>(BuyOfferCollectionName);
            _clock = clock;
            InitIndexes();
        }

        public async Task<List<BadgeBuyOffer>> FindAllBuyOffers(string? userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny)
        {
            FilterDefinition<BadgeBuyOffer> filter = Builders<BadgeBuyOffer>.Filter.Empty;
            if (userId != null)
                filter &= Builders<BadgeBuyOffer>.Filter.Eq(b => b.UserId, userId);
            if (species != null)
                filter &= Builders<BadgeBuyOffer>.Filter.Eq(b => b.Species, species);
            if (form != null)
                filter &= Builders<BadgeBuyOffer>.Filter.Eq(b => b.Form, form);
            if (source != null)
                filter &= Builders<BadgeBuyOffer>.Filter.Eq(b => b.Source, source);
            if (shiny == true)
                filter &= Builders<BadgeBuyOffer>.Filter.Eq(b => b.Shiny, true);
            else
                filter &= Builders<BadgeBuyOffer>.Filter.Ne(b => b.Shiny, true);

            return await BuyOfferCollection.Find(filter).ToListAsync();
        }

        public async Task<List<Badge>> FindAllBadgesForSale(string? userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny)
        {
            return await _badgeRepo.FindAllForSaleByCustom(userId, species, form, source, shiny);
        }

        public async Task<BadgeBuyOffer> CreateBuyOffer(string userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny, int price, int amount, Instant? createdAt = null)
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

            await BuyOfferCollection.InsertOneAsync(buyOffer);
            Debug.Assert(buyOffer.Id.Length > 0, "The MongoDB driver injected a generated ID");
            return buyOffer;
        }

        public async Task<Badge> CreateSellOffer(string userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny, int price)
        {
            List<Badge> notSellingOwnedByUser = await _badgeRepo.FindAllNotForSaleByCustom(userId, species, form, source, shiny);
            Badge toSell = SortBySpecialness(notSellingOwnedByUser).First();
            Badge selling = await _badgeRepo.SetBadgeSellPrice(toSell, price);
            return selling;
        }

        public async Task DeleteBuyOffer(string userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny, int amount)
        {
            List<BadgeBuyOffer> offers = await FindAllBuyOffers(userId, species, form, source, shiny);
            if(offers.Count() < amount)
                throw new ArgumentException(string.Format("Tried to cancel {0} offers but only {1} were found", amount, offers.Count));
            
            offers = offers.OrderByDescending(o => o.CreatedAt).ToList();
            for(int i=0; i < amount; i++)
            {
                await BuyOfferCollection.FindOneAndDeleteAsync(o => o.Id == offers[i].Id);
            }
        }
        public async Task DeleteSellOffer(string userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny, int amount)
        {
            List<Badge> badgesForSale = await FindAllBadgesForSale(userId, species, form, source, shiny);
            if (amount > badgesForSale.Count)
                throw new ArgumentException(string.Format("Tried to cancel {0} offers but only {1} were found", amount, badgesForSale.Count));

            badgesForSale = SortBySpecialness(badgesForSale);
            badgesForSale.Reverse();
            for(int i=0; i<amount; i++)
            {
                await _badgeRepo.SetBadgeSellPrice(badgesForSale[i], 0);
            }
        }

        public async Task<ImmutableList<IBadgeMarketRepo.BadgeSale>> ResolveBuyOffers(PkmnSpecies species, bool? shiny)
        {
            List<Badge> badgesForSale = await FindAllBadgesForSale(null, species, null, null, shiny);
            badgesForSale = badgesForSale.OrderByDescending(b => b.SellPrice).ThenBy(b => b.SellingSince).ToList();
            List<IBadgeMarketRepo.BadgeSale> soldBadges = new List<IBadgeMarketRepo.BadgeSale>();

            foreach (Badge badge in badgesForSale)
            {
                List<BadgeBuyOffer> buyOffers = await FindAllBuyOffers(null, species, null, null, shiny);
                buyOffers = buyOffers.Where(o => o.Price >= badge.SellPrice).ToList();
                buyOffers = buyOffers.OrderBy(o => o.WaitingSince).ToList();

                if(badge.SellPrice == null)
                    throw new InvalidOperationException("Tried to sell a badge with no sell price");

                foreach (BadgeBuyOffer offer in buyOffers)
                {
                    if (offer.UserId == badge.UserId)
                        continue; //user shouldn't sell to themself
                    if (badge.UserId == null)
                        throw new OwnedBadgeNotFoundException(badge);
                    if (((offer.Form != null) && (offer.Form != badge.Form))
                        || ((offer.Source != null) && (offer.Source != badge.Source)))
                        continue;

                    User? buyer = await _userRepo.FindById(offer.UserId);
                    User? seller = await _userRepo.FindById(badge.UserId);
                    if (buyer == null)
                        throw new UserNotFoundException<string>(offer.UserId);
                    if (seller == null)
                        throw new UserNotFoundException<string>(badge.UserId);
                    
                    soldBadges.Add(new IBadgeMarketRepo.BadgeSale(seller, buyer, badge, (long)badge.SellPrice));
                    await _tokenBank.PerformTransactions(
                        new Transaction<User>[]
                        {
                            new Transaction<User>(buyer, -1 * offer.Price, "BadgePurchase"),
                            new Transaction<User>(seller, offer.Price, "BadgeSale")
                        }
                    );
                    await _badgeRepo.TransferBadges(new List<Badge> { badge }.ToImmutableList(), buyer.Id, "BadgeSale", new Dictionary<string, object?>() { });
                    await ResetUserSellOffers(badge.UserId, badge.Species, badge.Form, badge.Shiny);
                    offer.decrement(_clock.GetCurrentInstant());
                    if (offer.Amount > 0)
                        await BuyOfferCollection.FindOneAndReplaceAsync(o => o.Id == offer.Id, offer);
                    else
                        await BuyOfferCollection.FindOneAndDeleteAsync(o => o.Id == offer.Id);
                    break; //this badge has been sold, ignore the rest of the offers
                }
            }
            return soldBadges.ToImmutableList();
        }
        /// <summary>
        /// Sorts badges according to the priority in which they should be sold.
        /// Current sorting rule: prioritize keeping 1 of each form, then sell newer badges first. Top priority badge will be selected for sale when fufilling offers.
        /// </summary>
        private static List<Badge> SortBySpecialness(IEnumerable<Badge> toSort)
        {
            IEnumerable<Badge> duplicates;
            IEnumerable<Badge> uniques = new List<Badge>();
            HashSet<string?> formNames = new HashSet<string?>();
            foreach (Badge b in toSort)
            {
                formNames.Add(b.Form);
            }

            foreach (string? form in formNames)
            {
                IEnumerable<Badge> ofSingleForm = toSort.Where(b => b.Form == form);
                ofSingleForm = ofSingleForm.OrderByDescending(b => b.CreatedAt);
                uniques = uniques.Append(ofSingleForm.Last());
            }

            duplicates = toSort.Except(uniques);
            duplicates = duplicates.OrderByDescending(b => b.CreatedAt);
            uniques = uniques.OrderByDescending(b => b.CreatedAt);

            IEnumerable<Badge> result = duplicates;
            foreach (Badge b in uniques)
            {
                result = result.Append(b);
            }
            return result.ToList();
        }

        /// <summary>
        /// Refresh user sell offers of a particular species and form. Moves them to the back of the line to fufill orders.
        /// </summary>
        private async Task ResetUserSellOffers(string userId, PkmnSpecies species, string? form, bool shiny)
        {
            List<Badge> forSale = await FindAllBadgesForSale(userId, species, form, null, shiny);
            foreach (Badge b in forSale)
            {
                if (b.SellPrice == null)
                    throw new OwnedBadgeNotFoundException(b);
                await _badgeRepo.SetBadgeSellPrice(b, (long)b.SellPrice);
            }
        }
    }
}
