using System;
using TPPCore.Irc;
using Xunit;

namespace TPPCore.Utils.Tests
{
    public class ClientIdTest
    {
        [Fact]
        public void TestParse()
        {
            var clientId = new ClientId();
            clientId.ParseFrom("Abc!Def@Xyz");

            Assert.Equal("Abc", clientId.Nickname);
            Assert.Equal("abc", clientId.NicknameLower);
            Assert.Equal("Def", clientId.User);
            Assert.Equal("Xyz", clientId.Host);
        }

        [Fact]
        public void TestEquals() {
            var clientId1 = new ClientId("Abc", "Def", "Xyz");
            var clientId2 = new ClientId("Abc", "Def", "Xyz");
            var clientId3 = new ClientId("aaa", "bbb", "Xyz");

            Assert.Equal(clientId1, clientId1);
            Assert.Equal(clientId1, clientId2);
            Assert.NotEqual(clientId1, clientId3);
        }
    }
}
