using System;
using Common;

namespace Persistence.Models
{
    // properties need setters for deserialization
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    public class Badge : PropertyEquatable<Badge>
    {
        /// <summary>
        /// A badge's unique ID.
        /// </summary>
        public string Id { get; private set; }
        protected override object EqualityId => Id;

        /// <summary>
        /// The user-ID this badge belongs to.
        /// If null, this badge belongs to nobody, e.g. if it was consumed in transmutation.
        /// </summary>
        public string? UserId { get; private set; }

        /// <summary>
        /// The badge's species.
        /// </summary>
        public PkmnSpecies Species { get; private set; }

        public enum BadgeSource
        {
            Pinball,
            RunCaught,
            Crate,
            ManualCreation,
            ManualDistribution,
            Breaking,
            Transmutation,
        }

        /// <summary>
        /// <see cref="BadgeSource"/> describing what event caused this badge to be created.
        /// </summary>
        public BadgeSource Source { get; private set; }

        /// <summary>
        /// Datetime this badge was created at.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        public Badge(string id, string? userId, PkmnSpecies species, BadgeSource source, DateTime createdAt)
        {
            Id = id;
            UserId = userId;
            Species = species;
            Source = source;
            CreatedAt = createdAt;
        }

        public override string ToString() => $"Badge({Species}@{UserId ?? "<nobody>"})";
    }
}
