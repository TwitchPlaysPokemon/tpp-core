using NodaTime;
using TPP.Common;

namespace TPP.Model;

public class Donation(
    int donationId,
    Instant createdAt,
    string userName,
    string? userId,
    int cents,
    string message
) : PropertyEquatable<Donation>
{
    /// <summary>
    /// Numeric unique donation ID, externally provided by the donation processor (e.g. StreamLabs).
    /// NOTE: This is an int because StreamLabs' donation IDs happen to be numeric. It should really be a string.
    /// </summary>
    public int DonationId { get; init; } = donationId;
    protected override object EqualityId => DonationId;

    /// <summary>
    /// The time the donation was created, as reported by the donation processor (e.g. StreamLabs).
    /// </summary>
    public Instant CreatedAt { get; init; } = createdAt;

    /// <summary>
    /// Username of the donor, as reported by the donation processor (e.g. StreamLabs).
    /// At the time of insertion into the DB, this gets converted to a known user ID, see the UserId field.
    /// </summary>
    public string UserName { get; init; } = userName;

    /// <summary>
    /// User-ID of the donor. May not be set in the case of an unknown user.
    /// </summary>
    public string? UserId { get; init; } = userId;

    /// <summary>
    /// Donation amount in USD cents.
    /// </summary>
    public int Cents { get; init; } = cents;

    /// <summary>
    /// Donation message, which can be null or empty string.
    /// </summary>
    public string? Message { get; init; } = message;
}
