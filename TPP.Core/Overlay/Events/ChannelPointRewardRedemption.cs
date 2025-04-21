using System.Runtime.Serialization;
using NodaTime;
using TPP.Model;

namespace TPP.Core.Overlay.Events;

[DataContract]
public enum RedemptionStatus
{
    [EnumMember(Value = "unknown")] Unknown,
    [EnumMember(Value = "unfulfilled")] Unfulfilled,
    [EnumMember(Value = "fulfilled")] Fulfilled,
    [EnumMember(Value = "canceled")] Canceled
}

[DataContract]
public struct RedemptionReward
{
    [DataMember(Name = "id")] public string Id { get; init; }
    [DataMember(Name = "title")] public string Title { get; init; }
    [DataMember(Name = "cost")] public int Cost { get; init; }
    [DataMember(Name = "promp")] public string Prompt { get; init; }
}

[DataContract]
public struct ChannelPointRewardRedemptionAdd : IOverlayEvent
{
    public string OverlayEventType => "channel_point_reward_redemption_add";

    [DataMember(Name = "id")] public string Id { get; init; }
    [DataMember(Name = "user")] public User User { get; init; }
    [DataMember(Name = "user_input")] public string UserInput { get; init; }
    [DataMember(Name = "status")] public RedemptionStatus Status { get; init; }
    [DataMember(Name = "reward")] public RedemptionReward Reward { get; init; }
    [DataMember(Name = "redeemed_at")] public Instant RedeemedAt { get; init; }
}

[DataContract]
public struct ChannelPointRewardRedemptionUpdate : IOverlayEvent
{
    public string OverlayEventType => "channel_point_reward_redemption_update";

    [DataMember(Name = "id")] public string Id { get; init; }
    [DataMember(Name = "user")] public User User { get; init; }
    [DataMember(Name = "user_input")] public string UserInput { get; init; }
    [DataMember(Name = "status")] public RedemptionStatus Status { get; init; }
    [DataMember(Name = "reward")] public RedemptionReward Reward { get; init; }
    [DataMember(Name = "redeemed_at")] public Instant RedeemedAt { get; init; }
}
