using System;
using Persistence.Models;

namespace Core.Tests
{
    public static class TestUtils
    {
        public static User MockUser(string name) => new User(Guid.NewGuid().ToString(),
            name, name, name.ToLower(), null, DateTime.UnixEpoch, DateTime.UnixEpoch, null, 0, 0);
    }
}
