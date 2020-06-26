using Core.Commands;
using Moq;
using NodaTime;
using NUnit.Framework;
using static Core.Tests.TestUtils;

namespace Core.Tests.Commands
{
    public class CooldownTest
    {
        [Test]
        public void TestGlobalCooldown()
        {
            Instant t1 = Instant.FromUnixTimeSeconds(1);
            Instant t2 = Instant.FromUnixTimeSeconds(2);
            Instant t3 = Instant.FromUnixTimeSeconds(3);
            var clockMock = new Mock<IClock>();
            var cooldown = new GlobalCooldown(clockMock.Object, Duration.FromSeconds(2));

            clockMock.Setup(clock => clock.GetCurrentInstant()).Returns(t1);
            Assert.IsTrue(cooldown.CheckLapsedThenReset());
            Assert.IsFalse(cooldown.CheckLapsedThenReset());

            clockMock.Setup(clock => clock.GetCurrentInstant()).Returns(t2);
            Assert.IsFalse(cooldown.CheckLapsedThenReset());

            clockMock.Setup(clock => clock.GetCurrentInstant()).Returns(t3);
            Assert.IsTrue(cooldown.CheckLapsedThenReset());
            Assert.IsFalse(cooldown.CheckLapsedThenReset());
        }

        [Test]
        public void TestPerUserCooldown()
        {
            Instant t1 = Instant.FromUnixTimeSeconds(1);
            Instant t2 = Instant.FromUnixTimeSeconds(2);
            Instant t3 = Instant.FromUnixTimeSeconds(3);
            var user1 = MockUser("user1");
            var user2 = MockUser("user2");
            var clockMock = new Mock<IClock>();
            var cooldown = new PerUserCooldown(clockMock.Object, Duration.FromSeconds(2));

            clockMock.Setup(clock => clock.GetCurrentInstant()).Returns(t1);
            Assert.IsTrue(cooldown.CheckLapsedThenReset(user1));
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user1));
            Assert.IsTrue(cooldown.CheckLapsedThenReset(user2));
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user2));

            clockMock.Setup(clock => clock.GetCurrentInstant()).Returns(t2);
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user1));
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user2));

            clockMock.Setup(clock => clock.GetCurrentInstant()).Returns(t3);
            Assert.IsTrue(cooldown.CheckLapsedThenReset(user1));
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user1));
            Assert.IsTrue(cooldown.CheckLapsedThenReset(user2));
            Assert.IsFalse(cooldown.CheckLapsedThenReset(user2));
        }
    }
}
