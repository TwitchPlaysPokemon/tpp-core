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
            Assert.That(cooldown.CheckLapsedThenReset(), Is.True);
            Assert.That(cooldown.CheckLapsedThenReset(), Is.False);

            clockMock.GetCurrentInstant().Returns(t2);
            Assert.That(cooldown.CheckLapsedThenReset(), Is.False);

            clockMock.GetCurrentInstant().Returns(t3);
            Assert.That(cooldown.CheckLapsedThenReset(), Is.True);
            Assert.That(cooldown.CheckLapsedThenReset(), Is.False);
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
            Assert.That(cooldown.CheckLapsedThenReset(user1), Is.True);
            Assert.That(cooldown.CheckLapsedThenReset(user1), Is.False);
            Assert.That(cooldown.CheckLapsedThenReset(user2), Is.True);
            Assert.That(cooldown.CheckLapsedThenReset(user2), Is.False);

            clockMock.GetCurrentInstant().Returns(t2);
            Assert.That(cooldown.CheckLapsedThenReset(user1), Is.False);
            Assert.That(cooldown.CheckLapsedThenReset(user2), Is.False);

            clockMock.GetCurrentInstant().Returns(t3);
            Assert.That(cooldown.CheckLapsedThenReset(user1), Is.True);
            Assert.That(cooldown.CheckLapsedThenReset(user1), Is.False);
            Assert.That(cooldown.CheckLapsedThenReset(user2), Is.True);
            Assert.That(cooldown.CheckLapsedThenReset(user2), Is.False);
        }
    }
}
