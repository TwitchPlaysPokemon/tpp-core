using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPP.Model;
using TPP.Persistence.MongoDB.Repos;
using MongoDB.Driver;
using NUnit.Framework;
using TPP.Common;
using TPP.Persistence.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos
{
    class BadgeMarketRepoTest : MongoTestBase
    {

        private Mock<IUserRepo> _mockUserRepo = null!;
        private Mock<IBank<User>> _mockBank = null!;
        private Mock<IBadgeRepo> _mockBadgeRepo = null!;

        public IBadgeMarketRepo CreateBadgeMarketRepo()
        {
            IMongoDatabase db = CreateTemporaryDatabase();
            _mockUserRepo = new Mock<IUserRepo>();
            _mockBank = new Mock<IBank<User>>();
            _mockBadgeRepo = new Mock<IBadgeRepo>();

            BadgeMarketRepo badgeMarketRepo = new BadgeMarketRepo(db, _mockBadgeRepo.Object, _mockUserRepo.Object, _mockBank.Object, Mock.Of<IClock>());
            return badgeMarketRepo;
        }

        internal class MockClock : IClock
        {
            public Instant FixedCurrentInstant = Instant.FromUnixTimeSeconds(1234567890);
            public Instant GetCurrentInstant() => FixedCurrentInstant;
        }

        [Test]
        public async Task CreateBuyOffer_write_then_read_are_equal()
        {
            string userId = "m4";
            PkmnSpecies species = PkmnSpecies.OfId("1");
            string form = "";
            Badge.BadgeSource source = Badge.BadgeSource.ManualCreation;
            bool shiny = true;
            int price = 999;
            int amount = 1;

            IBadgeMarketRepo badgeMarketRepo = CreateBadgeMarketRepo();

            _mockBadgeRepo.Setup(r => r.FindAllForSaleByCustom(null, species, null, null, shiny)).Returns(Task.FromResult(new List<Badge>()));

            BadgeBuyOffer offer = await badgeMarketRepo.CreateBuyOffer(userId, species, form, source, shiny, price, amount, Instant.MaxValue);

            Assert.That(userId, Is.EqualTo(offer.UserId));
            Assert.That(species, Is.EqualTo(offer.Species));
            Assert.That(form, Is.EqualTo(offer.Form));
            Assert.That(source, Is.EqualTo(offer.Source));
            Assert.That(shiny, Is.EqualTo(offer.Shiny));
            Assert.That(price, Is.EqualTo(offer.Price));
            Assert.That(amount, Is.EqualTo(offer.Amount));
        }

        [Test]
        public async Task CreateSellOffer_sells_newest_badge()
        {
            string userId = "thelegend27";
            PkmnSpecies species = PkmnSpecies.OfId("1");
            string form = "";
            Badge.BadgeSource source = Badge.BadgeSource.ManualCreation;
            bool shiny = true;
            int price = 999;
            Instant before = Instant.FromUtc(2000, 1, 1, 0, 0);
            Instant after = before.PlusNanoseconds(69);

            IBadgeMarketRepo badgeMarketRepo = CreateBadgeMarketRepo();

            Badge badgeA = new Badge("A", userId, species, source, before, form, shiny);
            Badge badgeB = new Badge("B", userId, species, source, after, form, shiny);
            Badge badgeBForSale = new Badge("B", userId, species, source, after, form, shiny) { SellPrice = price };
            List<Badge> notForSale = new List<Badge> { badgeA, badgeB };

            _mockBadgeRepo.Setup(m => m.FindAllNotForSaleByCustom(userId, species, form, source, shiny)).Returns(Task.FromResult(notForSale));
            _mockBadgeRepo.Setup(m => m.FindAllForSaleByCustom(null, species, null, null, shiny)).Returns(Task.FromResult(new List<Badge>()));
            _mockBadgeRepo.Setup(m => m.SetBadgeSellPrice(badgeB, price)).Returns(Task.FromResult(badgeBForSale));

            Badge selling = await badgeMarketRepo.CreateSellOffer(userId, species, form, source, shiny, price);

            Assert.That(after, Is.EqualTo(selling.CreatedAt));
        }

        [Test]
        public async Task CreateSellOffer_prioritizes_duplicates_of_forms_when_form_unspecified()
        {
            string userId = "shellosLuvr";
            PkmnSpecies species = PkmnSpecies.OfId("422");
            string formWest = "West Sea";
            string formEast = "East Sea";
            Badge.BadgeSource source = Badge.BadgeSource.RunCaught;
            bool shiny = false;
            Instant time = Instant.FromUtc(2006, 8, 28, 1, 0);
            int price = 999999;

            Badge shellosWest = new Badge("A", userId, species, source, time.PlusNanoseconds(9000), formWest, shiny);
            Badge shellosEastOlder = new Badge("B", userId, species, source, time.PlusNanoseconds(0), formEast, shiny);
            Badge shellosEastNewer = new Badge("C", userId, species, source, time.PlusNanoseconds(1), formEast, shiny);
            Badge shellosEastSelling = new Badge("C_sell", userId, species, source, time.PlusNanoseconds(1), formEast, shiny) { SellPrice = price };

            IBadgeMarketRepo badgeMarketRepo = CreateBadgeMarketRepo();

            List<Badge> notForSale = new List<Badge>() { shellosWest, shellosEastOlder, shellosEastNewer };

            _mockBadgeRepo.Setup(m => m.FindAllNotForSaleByCustom(userId, species, null, source, shiny)).Returns(Task.FromResult(notForSale));
            _mockBadgeRepo.Setup(m => m.FindAllForSaleByCustom(null, species, null, null, shiny)).Returns(Task.FromResult(new List<Badge>()));
            _mockBadgeRepo.Setup(m => m.SetBadgeSellPrice(shellosEastNewer, price)).Returns(Task.FromResult(shellosEastSelling));

            Badge selling = await badgeMarketRepo.CreateSellOffer(userId, species, null, source, shiny, price);

            Assert.That(selling.Id, Is.EqualTo(shellosEastSelling.Id));
        }

        [Test]
        public async Task ResolveBuyOffers_fills_outstanding_buy_offer()
        {
            string sellerId = "seller";
            string buyerId = "buyer";
            PkmnSpecies species = PkmnSpecies.OfId("1");
            Badge.BadgeSource source = Badge.BadgeSource.RunCaught;
            string? form = null;
            bool shiny = false;
            Instant time = Instant.FromUtc(1996, 2, 27, 0, 0);
            int price = 1;
            int amount = 1;
            User buyer = new User(buyerId, buyerId, buyerId, buyerId, null, Instant.MinValue, Instant.MinValue, null, 1000, price);
            User seller = new User(sellerId, sellerId, sellerId, sellerId, null, Instant.MinValue, Instant.MinValue, null, 1000, 0);
            Badge badgeA = new Badge("A", sellerId, species, source, time, form, shiny);
            Badge badgeASelling = new Badge("A", sellerId, species, source, time, form, shiny) { SellPrice = price, SellingSince = time };

            IBadgeMarketRepo badgeMarketRepo = CreateBadgeMarketRepo();

            List<Badge> sellerBadgesNotForSale = new List<Badge>() { badgeA };
            _mockBadgeRepo.Setup(m => m.FindAllNotForSaleByCustom(sellerId, species, form, source, shiny)).Returns(Task.FromResult(sellerBadgesNotForSale));
            _mockBadgeRepo.Setup(m => m.FindAllForSaleByCustom(null, species, null, null, shiny)).Returns(Task.FromResult(new List<Badge>()));
            _mockBadgeRepo.Setup(m => m.SetBadgeSellPrice(badgeA, price)).Returns(Task.FromResult(badgeASelling));
            Badge selling = await badgeMarketRepo.CreateSellOffer(sellerId, species, form, source, shiny, price);

            _mockBadgeRepo.Setup(m => m.FindAllForSaleByCustom(null, species, null, null, shiny)).Returns(Task.FromResult(new List<Badge>() { selling }));
            _mockUserRepo.Setup(m => m.FindById(buyerId)).Returns(Task.FromResult((User?)buyer));
            _mockUserRepo.Setup(m => m.FindById(sellerId)).Returns(Task.FromResult((User?)seller));
            _mockBadgeRepo.Setup(m => m.FindAllForSaleByCustom(sellerId, species, form, null, shiny)).Returns(Task.FromResult(new List<Badge>()));

            await badgeMarketRepo.CreateBuyOffer(buyerId, species, form, source, shiny, price, amount, time.PlusNanoseconds(2));
            
            List<BadgeBuyOffer> remainingBuyOffers = await badgeMarketRepo.FindAllBuyOffers(buyerId, species, form, source, shiny);
            Assert.That(remainingBuyOffers.Count, Is.EqualTo(1));

            var soldBadges = await badgeMarketRepo.ResolveBuyOffers(species, shiny);
            
            remainingBuyOffers = await badgeMarketRepo.FindAllBuyOffers(buyerId, species, form, source, shiny);
            Assert.That(remainingBuyOffers.Count, Is.EqualTo(0));
            Assert.That(soldBadges.Count, Is.EqualTo(1));
            Assert.That(soldBadges[0].seller, Is.EqualTo(seller));
            Assert.That(soldBadges[0].buyer, Is.EqualTo(buyer));
            Assert.That(soldBadges[0].soldBadge, Is.EqualTo(badgeASelling));
            Assert.That(soldBadges[0].price, Is.EqualTo(price));
        }

        [Test]
        public async Task DeleteBuyOffer_removes_outstanding_buy_offer()
        {
            string buyerId = "buyer";
            PkmnSpecies species = PkmnSpecies.OfId("1");
            Badge.BadgeSource? source = null;
            string? form = null;
            bool shiny = false;
            Instant time = Instant.FromUtc(0,1,1,0,0);
            int price = 1;
            int amount = 1;

            IBadgeMarketRepo badgeMarketRepo = CreateBadgeMarketRepo();
            _mockBadgeRepo.Setup(r => r.FindAllForSaleByCustom(null, species, null, null, shiny)).Returns(Task.FromResult(new List<Badge>()));

            await badgeMarketRepo.CreateBuyOffer(buyerId, species, form, source, shiny, price, amount, time);
            Assert.That(badgeMarketRepo.FindAllBuyOffers(buyerId, species, form, source, shiny).Result.Count, Is.EqualTo(1));

            await badgeMarketRepo.DeleteBuyOffer(buyerId, species, form, source, shiny, amount);
            Assert.That(badgeMarketRepo.FindAllBuyOffers(buyerId, species, form, source, shiny).Result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task DeleteSellOffer_removes_outstanding_sell_offer()
        {
            string sellerId = "buyer";
            PkmnSpecies species = PkmnSpecies.OfId("1");
            Badge.BadgeSource source = Badge.BadgeSource.Pinball;
            string? form = null;
            bool shiny = false;
            Instant time = Instant.FromUtc(0,1,1,0,0);
            long price = 1;
            Badge badgeForSale = new Badge("A", sellerId, species, source, time, form, shiny){ SellingSince=time, SellPrice=price};
            Badge badgeNotForSale = new Badge("A", sellerId, species, source, time, form, shiny);

            IBadgeMarketRepo badgeMarketRepo = CreateBadgeMarketRepo();

            _mockBadgeRepo.Setup(r => r.FindAllForSaleByCustom(sellerId, species, form, source, shiny)).Returns(Task.FromResult(new List<Badge>() { badgeForSale } ));
            _mockBadgeRepo.Setup(r => r.SetBadgeSellPrice(badgeForSale, 0)).Returns(Task.FromResult(badgeNotForSale));
            await badgeMarketRepo.DeleteSellOffer(sellerId, species, form, source, shiny, 1);

            _mockBadgeRepo.Setup(r => r.FindAllForSaleByCustom(sellerId, species, form, source, shiny)).Returns(Task.FromResult(new List<Badge>()));
            List<Badge> badgesForSale = await badgeMarketRepo.FindAllBadgesForSale(sellerId, species, form, source, shiny);

            Assert.That(badgesForSale.Count, Is.EqualTo(0));
        }
    }
}
