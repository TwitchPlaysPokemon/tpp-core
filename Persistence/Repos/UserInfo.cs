using System;

namespace Persistence.Repos
{
    public struct UserInfo
    {
        public string Id { get; }
        public string TwitchDisplayName { get; }
        public string SimpleName { get; }
        public string? Color { get; }
        public bool FromMessage { get; }
        public DateTime UpdatedAt { get; }

        public UserInfo(
            string id,
            string twitchDisplayName,
            string simpleName,
            string? color,
            bool fromMessage = false,
            DateTime? updatedAt = null)
        {
            Id = id;
            TwitchDisplayName = twitchDisplayName;
            SimpleName = simpleName;
            Color = color;
            FromMessage = fromMessage;
            UpdatedAt = updatedAt ?? DateTime.UtcNow;
        }
    }
}
