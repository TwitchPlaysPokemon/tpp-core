using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPP.Common;

namespace TPP.Persistence.Models
{
    public class BadgeBuyOffer
    {
        /// <summary>
        /// Unique Id.
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// The ID of the user that created the buy offer.
        /// </summary>
        public string UserId { get; init; }

        /// <summary>
        /// The species of pokemon to buy.
        /// </summary>
        public PkmnSpecies Species { get; init; }

        /// <summary>
        /// The form of pokemon to buy.
        /// </summary>
        public int? Form { get; init; }

        /// <summary>
        /// The source of the badge to buy.
        /// </summary>
        public Badge.BadgeSource? Source { get; init; }

        /// <summary>
        /// Is the offer seeking shiny badges.
        /// </summary>
        public bool? Shiny { get; init; }

        /// <summary>
        /// How much to pay for each badge.
        /// </summary>
        public int Price { get; init; }

        /// <summary>
        /// The number of badges to buy.
        /// </summary>
        public int Amount { get; private set; }

        /// <summary>
        /// When the buy offer was created.
        /// </summary>
        public Instant CreatedAt { get; init; }

        /// <summary>
        /// When this offer was last updated.
        /// </summary>
        public Instant WaitingSince { get; private set; }

        //duration and expires_at depricated from old core

        public BadgeBuyOffer(string id, string userId, PkmnSpecies species, int? form, Badge.BadgeSource? source, bool? shiny, int price, int amount, Instant createdAt)
        {
            Id = id;
            UserId = userId;
            Species = species;
            Form = form;
            Source = source;
            Shiny = shiny;
            Price = price;
            Amount = amount;
            CreatedAt = createdAt;
            WaitingSince = createdAt;
        }

        /// <summary>
        /// Decrement the amount to buy.
        /// </summary>
        public void decrement(Instant decrementedAt)
        {
            if (Amount <= 0)
                throw new InvalidOperationException("The buy offer has no badges remaining, and cannot be decremented further.");
            Amount--;
            WaitingSince = decrementedAt;
        }
    }
}
