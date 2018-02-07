using System;
using TPPCore.Irc;
using Xunit;

namespace TPPCore.Utils.Tests
{
    public class PrefixTest
    {
        [Fact]
        public void TestParseClient()
        {
            var prefix = new Prefix();
            prefix.ParseFrom("hello!abc@def");

            Assert.NotNull(prefix.ClientId);
            Assert.Null(prefix.Server);
        }

        [Fact]
        public void TestParseServer()
        {
            var prefix = new Prefix();
            prefix.ParseFrom("irc.example.com");

            Assert.Null(prefix.ClientId);
            Assert.NotNull(prefix.Server);
        }
    }
}
