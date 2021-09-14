using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPP.Persistence.Models;
using TPP.Persistence.MongoDB.Repos;
using MongoDB.Driver;
using NUnit.Framework;
using TPP.Common;
using TPP.Persistence.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos
{
    class BadgeBuyOfferRepoTest : MongoTestBase
    {

        private Mock<IUserRepo> _mockUserRepo = null!;
        private Mock<IBank<User>> _mockBank = null!;
        private Mock<IBadgeRepo> _mockBadgeRepo = null!;

        public IBadgeBuyOfferRepo CreateBadgeBuyOfferRepo()
        {
            IMongoDatabase db = CreateTemporaryDatabase();
            _mockUserRepo = new Mock<IUserRepo>();
            _mockBank = new Mock<IBank<User>>();
            _mockBadgeRepo = new Mock<IBadgeRepo>();

            BadgeBuyOfferRepo badgeBuyOfferRepo = new BadgeBuyOfferRepo(db, _mockBadgeRepo.Object, _mockUserRepo.Object, _mockBank.Object, Mock.Of<IClock>());
            return badgeBuyOfferRepo;
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

            IBadgeBuyOfferRepo badgeBuyOfferRepo = CreateBadgeBuyOfferRepo();

            BadgeBuyOffer offer = await badgeBuyOfferRepo.CreateBuyOffer(userId, species, form, source, shiny, price, amount, Instant.MaxValue);

            Assert.AreEqual(userId, offer.UserId);
            Assert.AreEqual(species, offer.Species);
            Assert.AreEqual(form, offer.Form);
            Assert.AreEqual(source, offer.Source);
            Assert.AreEqual(shiny, offer.Shiny);
            Assert.AreEqual(price, offer.Price);
            Assert.AreEqual(amount, offer.Amount);
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

            IBadgeBuyOfferRepo badgeBuyOfferRepo = CreateBadgeBuyOfferRepo();

            Badge badgeA = new Badge("A", userId, species, source, before, form, shiny);
            Badge badgeB = new Badge("B", userId, species, source, after, form, shiny);
            Badge badgeBForSale = new Badge("B", userId, species, source, after, form, shiny) { SellPrice = price };
            List<Badge> notForSale = new List<Badge> { badgeA, badgeB };

            _mockBadgeRepo.Setup(m => m.FindAllNotForSaleByCustom(userId, species, form, source, shiny)).Returns(Task.FromResult(notForSale));
            _mockBadgeRepo.Setup(m => m.FindAllForSaleByCustom(null, species, null, null, shiny)).Returns(Task.FromResult(new List<Badge>()));
            _mockBadgeRepo.Setup(m => m.SetBadgeSellPrice(badgeB, price)).Returns(Task.FromResult(badgeBForSale));

            Badge selling = await badgeBuyOfferRepo.CreateSellOffer(userId, species, form, source, shiny, price);

            Assert.AreEqual(after, selling.CreatedAt);
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

            IBadgeBuyOfferRepo badgeBuyOfferRepo = CreateBadgeBuyOfferRepo();

            List<Badge> notForSale = new List<Badge>() { shellosWest, shellosEastOlder, shellosEastNewer };

            _mockBadgeRepo.Setup(m => m.FindAllNotForSaleByCustom(userId, species, null, source, shiny)).Returns(Task.FromResult(notForSale));
            _mockBadgeRepo.Setup(m => m.FindAllForSaleByCustom(null, species, null, null, shiny)).Returns(Task.FromResult(new List<Badge>()));
            _mockBadgeRepo.Setup(m => m.SetBadgeSellPrice(shellosEastNewer, price)).Returns(Task.FromResult(shellosEastSelling));

            Badge selling = await badgeBuyOfferRepo.CreateSellOffer(userId, species, null, source, shiny, price);

            Assert.AreEqual(selling.Id, shellosEastSelling.Id);
        }

        [Test]
        public async Task new_buy_offer_is_filled_if_possible()
        {
            string sellerId = "seller";
            string buyerId = "buyer";
            PkmnSpecies species = PkmnSpecies.OfId("1");
            Badge.BadgeSource source = Badge.BadgeSource.RunCaught;
            string? form = null;
            bool shiny = false;
            Instant time = Instant.FromUtc(1996, 2, 27, 0, 0);
            int price = 1;

            Badge badgeA = new Badge("A", sellerId, species, source, time, form, shiny);
            Badge badgeASelling = new Badge("A", sellerId, species, source, time, form, shiny) { SellPrice = price, SellingSince = time };

            IBadgeBuyOfferRepo badgeBuyOfferRepo = CreateBadgeBuyOfferRepo();

            List<Badge> sellerBadgesNotForSale = new List<Badge>() { badgeA };
            _mockBadgeRepo.Setup(m => m.FindAllNotForSaleByCustom(sellerId, species, form, source, shiny)).Returns(Task.FromResult(sellerBadgesNotForSale));
            _mockBadgeRepo.Setup(m => m.FindAllForSaleByCustom(null, species, null, null, shiny)).Returns(Task.FromResult(new List<Badge>()));
            _mockBadgeRepo.Setup(m => m.SetBadgeSellPrice(badgeA, price)).Returns(Task.FromResult(badgeASelling));
            await badgeBuyOfferRepo.CreateSellOffer(sellerId, species, null, source, shiny, price);
            await badgeBuyOfferRepo.CreateBuyOffer(buyerId, species, sellerId, source, shiny, price, 1, time.PlusNanoseconds(2));

            int remainingBuyOffers = badgeBuyOfferRepo.FindAllByCustom(buyerId, species, null, source, shiny).Result.Count();
            Assert.AreEqual(0, remainingBuyOffers);
        }
    }
}
