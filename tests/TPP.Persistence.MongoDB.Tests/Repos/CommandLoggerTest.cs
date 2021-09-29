using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MongoDB.Driver;
using Moq;
using NodaTime;
using NUnit.Framework;
using TPP.Model;
using TPP.Persistence.MongoDB.Repos;

namespace TPP.Persistence.MongoDB.Tests.Repos;

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
        Assert.That(written.UserId, Is.EqualTo(userId));
        Assert.That(written.Command, Is.EqualTo(command));
        Assert.That(written.Args, Is.EqualTo(args));
        Assert.That(written.Response, Is.EqualTo(response));
        Assert.That(written.Timestamp, Is.EqualTo(timestamp));
        Assert.NotNull(written.Id);

        // read from db
        List<CommandLog> allItems = await repo.Collection.Find(FilterDefinition<CommandLog>.Empty).ToListAsync();
        Assert.That(allItems.Count, Is.EqualTo(1));
        CommandLog read = allItems[0];
        Assert.That(read, Is.EqualTo(written));
        Assert.That(read.UserId, Is.EqualTo(userId));
        Assert.That(read.Command, Is.EqualTo(command));
        Assert.That(read.Args, Is.EqualTo(args));
        Assert.That(read.Response, Is.EqualTo(response));
        Assert.That(read.Timestamp, Is.EqualTo(timestamp));
    }
}
