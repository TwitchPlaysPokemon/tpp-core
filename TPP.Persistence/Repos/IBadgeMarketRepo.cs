using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPP.Common;
using TPP.Persistence.Models;
using System.Collections.Immutable;

namespace TPP.Persistence.Repos
{
    public interface IBadgeMarketRepo
    {
        record BadgeSale(User seller, User buyer, Badge soldBadge, long price);
        Task<List<BadgeBuyOffer>> FindAllBuyOffers(string? userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny);
        Task<List<Badge>> FindAllBadgesForSale(string? userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny);
        Task<BadgeBuyOffer> CreateBuyOffer(string userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny, int price, int amount, Instant? createdAt);
        Task<Badge> CreateSellOffer(string userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny, int price);
        Task DeleteBuyOffer(string userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny, int amount);
        Task DeleteSellOffer(string userId, PkmnSpecies species, string? form, Badge.BadgeSource? source, bool? shiny, int amount);
        Task<ImmutableList<BadgeSale>> ResolveBuyOffers(PkmnSpecies species, bool? shiny);
    }
}
