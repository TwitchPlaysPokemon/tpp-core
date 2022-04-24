using NodaTime;

namespace TPP.Model;

public record SidePick(string? UserId, string? Side, Instant PickedAt);
