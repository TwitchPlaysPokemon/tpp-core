using System;
using NodaTime;
using Persistence.Models;

namespace Core.Tests
{
    public static class TestUtils
    {
        public static User MockUser(string name) => new User(
            id: Guid.NewGuid().ToString(),
            name: name, twitchDisplayName: name, simpleName: name.ToLower(), color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0),
            lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null,
            pokeyen: 0, tokens: 0);
    }
}
