using System;
using NodaTime;
using NSubstitute;
using NUnit.Framework;
using TPP.Core.Commands;
using TPP.Model;

namespace TPP.Core.Tests.Commands
{
    public class CooldownTest
    {
        private static User MockUser(string name) => new User(
            id: Guid.NewGuid().ToString(),
            name: name, twitchDisplayName: "☺" + name, simpleName: name.ToLower(), color: null,
            firstActiveAt: Instant.FromUnixTimeSeconds(0), lastActiveAt: Instant.FromUnixTimeSeconds(0),
            lastMessageAt: null, pokeyen: 0, tokens: 0);

        [Test]
        public void TestGlobalCooldown()
        {
            Instant t1 = Instant.FromUnixTimeSeconds(1);
            Instant t2 = Instant.FromUnixTimeSeconds(2);
            Instant t3 = Instant.FromUnixTimeSeconds(3);
            var clockMock = Substitute.For<IClock>();
            var cooldown = new GlobalCooldown(clockMock, Duration.FromSeconds(2));

            clockMock.GetCurrentInstant().Returns(t1);
            Assert.IsTrue(cooldown.CheckLapsedThenReset());
            Assert.IsFalse(cooldown.CheckLapsedThenReset());

            clockMock.GetCurrentInstant().Returns(t2);
            Assert.IsFalse(cooldown.CheckLapsedThenReset());

            clockMock.GetCurrentInstant().Returns(t3);
            Assert.IsTrue(cooldown.CheckLapsedThenReset());
            Assert.IsFalse(cooldown.CheckLapsedThenReset());
        }

        [Test]
        public void TestPerUserCooldown()
        {
            Instant t1 = Instant.FromUnixTimeSeconds(1);
            Instant t2 = Instant.FromUnixTimeSeconds(2);
            Instant t3 = Instant.FromUnixTimeSeconds(3);
            var user1 = MockUser("User1");
            var user2 = MockUser("User2");
            var clockMock = Substitute.For<IClock>();
            var cooldown = new PerUserCooldown(clockMock, Duration.FromSeconds(2));

            clockMock.GetCurrentInstant().Returns(t1);
            Assert.IsTrue(cooldown.CheckLapsedThenReset(user1));
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user1));
            Assert.IsTrue(cooldown.CheckLapsedThenReset(user2));
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user2));

            clockMock.GetCurrentInstant().Returns(t2);
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user1));
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user2));

            clockMock.GetCurrentInstant().Returns(t3);
            Assert.IsTrue(cooldown.CheckLapsedThenReset(user1));
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user1));
            Assert.IsTrue(cooldown.CheckLapsedThenReset(user2));
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user2));
        }
    }
}
