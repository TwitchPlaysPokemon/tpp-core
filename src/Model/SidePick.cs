using NodaTime;

namespace Model;

public record SidePick(string? UserId, string? Side, Instant PickedAt);
