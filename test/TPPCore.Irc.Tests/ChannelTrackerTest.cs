using System;
using TPPCore.Irc;
using Xunit;

namespace TPPCore.Utils.Tests
{
    public class ChannelTrackerTest
    {
        [Fact]
        public void TestJoinPart()
        {
            var tracker = new ChannelTracker();
            var someoneClientId = new ClientId("Someone", "user", "example.com");

            var message = new Message("JOIN", new[] { "#Test" });
            message.Prefix.ClientId = someoneClientId;

            tracker.UpdateFromMessage(message);
            Assert.True(tracker.ContainsKey("#test"));

            var channel = tracker["#test"];
            Assert.True(channel.ContainsKey("someone"));

            var user = channel["someone"];
            Assert.Equal("someone", user.ClientId.NicknameLower);

            message = new Message("PART", new[] { "#Test" });
            message.Prefix.ClientId = someoneClientId;

            tracker.UpdateFromMessage(message);
            Assert.False(channel.ContainsKey("someone"));
        }

        [Fact]
        public void TestExtraJoin()
        {
            var tracker = new ChannelTracker();
            var someoneClientId = new ClientId("Someone", "user", "example.com");

            var message = new Message("JOIN", new[] { "#Test" });
            message.Prefix.ClientId = someoneClientId;

            tracker.UpdateFromMessage(message);
            tracker.UpdateFromMessage(message);  // double

            Assert.True(tracker["#test"].ContainsKey("someone"));
        }

        [Fact]
        public void TestMissingPart()
        {
            var tracker = new ChannelTracker();
            var someoneClientId = new ClientId("Someone", "user", "example.com");

            var message = new Message("PART", new[] { "#Test" });
            message.Prefix.ClientId = someoneClientId;

            tracker.UpdateFromMessage(message);
            Assert.False(tracker["#test"].ContainsKey("someone"));
        }

        [Fact]
        public void TestQuit()
        {
            var tracker = new ChannelTracker();
            var someoneClientId = new ClientId("Someone", "user", "example.com");

            var message = new Message("JOIN", new[] { "#Test" });
            message.Prefix.ClientId = someoneClientId;

            tracker.UpdateFromMessage(message);

            message = new Message("QUIT");
            message.Prefix.ClientId = someoneClientId;

            tracker.UpdateFromMessage(message);
            Assert.False(tracker["#test"].ContainsKey("someone"));
        }

        [Fact]
        public void TestMissingQuit()
        {
            var tracker = new ChannelTracker();
            var someoneClientId = new ClientId("Someone", "user", "example.com");

            var message = new Message("JOIN", new[] { "#Test" });
            message.Prefix.ClientId = someoneClientId;
            tracker.UpdateFromMessage(message);

            message = new Message("PART", new[] { "#Test" });
            message.Prefix.ClientId = someoneClientId;
            tracker.UpdateFromMessage(message);

            message = new Message("QUIT");
            message.Prefix.ClientId = someoneClientId;

            tracker.UpdateFromMessage(message);

            Assert.False(tracker["#test"].ContainsKey("someone"));
        }

        [Fact]
        public void TestKick()
        {
            var tracker = new ChannelTracker();
            var meClientId = new ClientId("Me", "user", "example.com");
            var someoneClientId = new ClientId("Someone", "user", "example.com");

            var message = new Message("JOIN", new[] { "#Test" });
            message.Prefix.ClientId = someoneClientId;

            tracker.UpdateFromMessage(message);

            message = new Message("KICK", new[] { "#Test", "someone"});
            message.Prefix.ClientId = meClientId;

            tracker.UpdateFromMessage(message);
            Assert.False(tracker["#test"].ContainsKey("someone"));
        }

        [Fact]
        public void TestMissingKick()
        {
            var tracker = new ChannelTracker();
            var meClientId = new ClientId("Me", "user", "example.com");
            var someoneClientId = new ClientId("Someone", "user", "example.com");

            var message = new Message("KICK", new[] { "#Test", "someone"});
            message.Prefix.ClientId = meClientId;
            tracker.UpdateFromMessage(message);

            Assert.False(tracker["#test"].ContainsKey("someone"));
        }

        [Fact]
        public void TestNickChange()
        {
            var tracker = new ChannelTracker();
            var someoneClientId = new ClientId("Someone", "user", "example.com");

            var message = new Message("JOIN", new[] { "#Test" });
            message.Prefix.ClientId = someoneClientId;

            tracker.UpdateFromMessage(message);

            message = new Message("NICK", new[] { "SomeoneElse" });
            message.Prefix.ClientId = someoneClientId;
            tracker.UpdateFromMessage(message);

            Assert.False(tracker["#test"].ContainsKey("someone"));
            Assert.True(tracker["#test"].ContainsKey("someoneelse"));

            var user = tracker["#test"]["someoneelse"];
            Assert.Equal("someoneelse", user.ClientId.NicknameLower);
        }

        [Fact]
        public void TestNameReply()
        {
            var tracker = new ChannelTracker();
            var message = new Message(
                NumericalReplyCodes.RPL_NAMREPLY,
                new[] { "someone", "=", "#test"} ,
                "@chanOwner +someoneElse bill"
            );

            tracker.UpdateFromMessage(message);

            Assert.True(tracker.ContainsKey("#test"));
            Assert.True(tracker["#test"].ContainsKey("chanowner"));
            Assert.True(tracker["#test"].ContainsKey("someoneelse"));
            Assert.True(tracker["#test"].ContainsKey("bill"));
        }
    }
}
