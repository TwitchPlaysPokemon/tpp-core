using NodaTime;
using TPP.Common;

namespace TPP.Persistence;

public readonly struct UserInfo
{
    public string Id { get; }
    public string TwitchDisplayName { get; }
    public string SimpleName { get; }
    public HexColor? Color { get; }
    public bool FromMessage { get; }
    public Instant UpdatedAt { get; }

    public UserInfo(
        string id,
        string twitchDisplayName,
        string simpleName,
        HexColor? color = null,
        bool fromMessage = false,
        Instant? updatedAt = null)
    {
        Id = id;
        TwitchDisplayName = twitchDisplayName;
        SimpleName = simpleName;
        Color = color;
        FromMessage = fromMessage;
        UpdatedAt = updatedAt ?? SystemClock.Instance.GetCurrentInstant();
    }
}
