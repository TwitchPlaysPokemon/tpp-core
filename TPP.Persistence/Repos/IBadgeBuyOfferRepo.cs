using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPP.Common;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public interface IBadgeBuyOfferRepo
    {
        Task<List<BadgeBuyOffer>> FindAllByCustom(string? userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny);
        Task<BadgeBuyOffer> CreateBuyOffer(string userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny, int price, int amount, Instant? createdAt);
        Task<Badge> CreateSellOffer(string userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny, int price);
    }
}
