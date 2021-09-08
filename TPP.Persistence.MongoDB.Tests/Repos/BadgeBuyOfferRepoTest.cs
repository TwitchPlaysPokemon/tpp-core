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
        public BadgeBuyOfferRepo CreateBadgeBuyOfferRepo()
        {
            IMongoDatabase db = CreateTemporaryDatabase();
            BadgeRepo mockBadgeRepo = new BadgeRepo(db, Mock.Of<IMongoBadgeLogRepo>(), Mock.Of<IClock>());
            return new BadgeBuyOfferRepo(db, mockBadgeRepo, Mock.Of<IClock>());
        }

        internal class MockClock : IClock
        {
            public Instant FixedCurrentInstant = Instant.FromUnixTimeSeconds(1234567890);
            public Instant GetCurrentInstant() => FixedCurrentInstant;
        }

        [Test]
        public async Task write_then_read_are_equal()
        {
            string userId = "m4";
            PkmnSpecies species = PkmnSpecies.OfId("1");
            int form = 0;
            Badge.BadgeSource source = Badge.BadgeSource.ManualCreation;
            bool shiny = true;
            int price = 999;
            int amount = 1;

            IBadgeBuyOfferRepo badgeBuyOfferRepo = CreateBadgeBuyOfferRepo();

            BadgeBuyOffer offer =  await badgeBuyOfferRepo.CreateBuyOffer(userId, species, form, source, shiny, price, amount, Instant.MaxValue);

            Assert.AreEqual(userId, offer.UserId);
            Assert.AreEqual(species, offer.Species);
            Assert.AreEqual(form, offer.Form);
            Assert.AreEqual(source, offer.Source);
            Assert.AreEqual(shiny, offer.Shiny);
            Assert.AreEqual(price, offer.Price);
            Assert.AreEqual(amount, offer.Amount);
        }
    }
}
