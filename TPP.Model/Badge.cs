using NodaTime;
using TPP.Common;

namespace TPP.Model;

public class Badge : PropertyEquatable<Badge>
{
    /// <summary>
    /// A badge's unique ID.
    /// </summary>
    public string Id { get; init; }
    protected override object EqualityId => Id;

    /// <summary>
    /// The user-ID this badge belongs to.
    /// If null, this badge belongs to nobody, e.g. if it was consumed in transmutation.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// The badge's species.
    /// </summary>
    public PkmnSpecies Species { get; init; }

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
    public BadgeSource Source { get; init; }

    /// <summary>
    /// Instant this badge was created at.
    /// </summary>
    public Instant CreatedAt { get; init; }

    /// If this badge is on sale, for how much.
    public long? SellPrice { get; init; }
    /// If this badge is on sale, since when.
    public Instant? SellingSince { get; init; }

    public Badge(
        string id,
        string? userId,
        PkmnSpecies species,
        BadgeSource source,
        Instant createdAt)
    {
        Id = id;
        UserId = userId;
        Species = species;
        Source = source;
        CreatedAt = createdAt;
    }

    public override string ToString() => $"Badge({Species}@{UserId ?? "<nobody>"})";
}
