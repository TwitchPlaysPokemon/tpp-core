using System;
using TPPCore.Irc;
using Xunit;

namespace TPPCore.Utils.Tests
{
    public class CtcpMessageTest
    {
        [Fact]
        public void TestParseParams()
        {
            var ctcpMessage = new CtcpMessage();
            ctcpMessage.ParseFrom("\u0001ACTION writes some specs!\u0001");

            Assert.Equal("ACTION", ctcpMessage.Command);
            Assert.Equal("writes some specs!", ctcpMessage.Parameters);
        }

        [Fact]
        public void TestParseWithoutTrailingDelim()
        {
            var ctcpMessage = new CtcpMessage();
            ctcpMessage.ParseFrom("\u0001ACTION writes some specs!");

            Assert.Equal("ACTION", ctcpMessage.Command);
            Assert.Equal("writes some specs!", ctcpMessage.Parameters);
        }

        [Fact]
        public void TestParseNoParams()
        {
            var ctcpMessage = new CtcpMessage();
            ctcpMessage.ParseFrom("\u0001VERSION\u0001");

            Assert.Equal("VERSION", ctcpMessage.Command);
            Assert.Equal("", ctcpMessage.Parameters);
        }

        [Fact]
        public void TestNoCommand()
        {
            var ctcpMessage = new CtcpMessage();
            ctcpMessage.ParseFrom("\u0001\u0001");

            Assert.Equal("", ctcpMessage.Command);
            Assert.Equal("", ctcpMessage.Parameters);
        }

        [Fact]
        public void TestOldStyle()
        {
            var ctcpMessage = new CtcpMessage();
            ctcpMessage.ParseFrom("\u0001ACTION stop\u0001\u0001ACTION spamming\u0001");

            Assert.Equal("ACTION", ctcpMessage.Command);
            Assert.Equal("stop\u0001\u0001ACTION spamming", ctcpMessage.Parameters);
        }

        [Fact]
        public void TestToStringParams() {
            var ctcpMessage = new CtcpMessage("ACTION", "waves");
            Assert.Equal("\u0001ACTION waves\u0001", ctcpMessage.ToString());
        }

        [Fact]
        public void TestToStringNoParams() {
            var ctcpMessage = new CtcpMessage("VERSION");
            Assert.Equal("\u0001VERSION\u0001", ctcpMessage.ToString());
        }
    }
}
