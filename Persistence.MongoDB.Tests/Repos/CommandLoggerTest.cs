using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Driver;
using Moq;
using NodaTime;
using NUnit.Framework;
using Persistence.Models;
using Persistence.MongoDB.Repos;

namespace Persistence.MongoDB.Tests.Repos
{
    public class CommandLoggerTest : MongoTestBase
    {
        [Test]
        public async Task persists_successfully()
        {
            Mock<IClock> clock = new();
            CommandLogger repo = new(CreateTemporaryDatabase(), clock.Object);
            const string userId = "123";
            const string command = "irc line text";
            IImmutableList<string> args = ImmutableList.Create("a", "b", "c");
            const string response = "message text";
            Instant timestamp = Instant.FromUnixTimeSeconds(123);
            clock.Setup(c => c.GetCurrentInstant()).Returns(timestamp);

            // persist to db
            CommandLog written = await repo.Log(userId, command, args, response);
            Assert.AreEqual(userId, written.UserId);
            Assert.AreEqual(command, written.Command);
            Assert.AreEqual(args, written.Args);
            Assert.AreEqual(response, written.Response);
            Assert.AreEqual(timestamp, written.Timestamp);
            Assert.NotNull(written.Id);

            // read from db
            List<CommandLog> allItems = await repo.Collection.Find(FilterDefinition<CommandLog>.Empty).ToListAsync();
            Assert.AreEqual(1, allItems.Count);
            CommandLog read = allItems[0];
            Assert.AreEqual(written, read);
            Assert.AreEqual(userId, read.UserId);
            Assert.AreEqual(command, read.Command);
            Assert.AreEqual(args, read.Args);
            Assert.AreEqual(response, read.Response);
            Assert.AreEqual(timestamp, read.Timestamp);
        }
    }
}
