namespace TPP.Twitch.EventSub.Notifications;

/// <summary>
/// Basic information about the reward that was redeemed, at the time it was redeemed.
/// </summary>
/// <param name="Id">The reward identifier.</param>
/// <param name="Title">The reward name.</param>
/// <param name="Cost">The reward cost.</param>
/// <param name="Prompt">The reward description.</param>
public record RedemptionReward(
    string Id,
    string Title,
    int Cost,
    string Prompt
);

/// <summary>
/// The status of the redemption. Possible values:
/// - unknown
/// - unfulfilled
/// - fulfilled
/// - canceled
/// </summary>
public enum RedemptionStatus
{
    Unknown,
    Unfulfilled,
    Fulfilled,
    Canceled
}
