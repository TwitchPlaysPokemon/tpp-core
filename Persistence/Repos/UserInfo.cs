using NodaTime;

namespace Persistence.Repos
{
    public readonly struct UserInfo
    {
        public string Id { get; }
        public string TwitchDisplayName { get; }
        public string SimpleName { get; }
        public string? Color { get; }
        public bool FromMessage { get; }
        public Instant UpdatedAt { get; }

        public UserInfo(
            string id,
            string twitchDisplayName,
            string simpleName,
            string? color,
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
}
