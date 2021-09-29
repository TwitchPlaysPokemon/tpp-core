using NodaTime;
using TPP.Common;

namespace TPP.Persistence;

public readonly record struct UserInfo
(
    string Id,
    string TwitchDisplayName,
    string SimpleName,
    HexColor? Color = null,
    bool FromMessage = false,
    Instant? UpdatedAt = null
);
