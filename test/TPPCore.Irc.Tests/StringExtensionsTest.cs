using System;
using TPPCore.Irc;
using Xunit;

namespace TPPCore.Utils.Tests
{
    public class StringExtensionsTest
    {
        [Fact]
        public void TestToLowerIrc()
        {
            Assert.Equal("abc{}|", StringExtensions.ToLowerIrc("Abc[]\\"));
        }

        [Fact]
        public void TestUnsafeChars()
        {
            Assert.False(StringExtensions.ContainsUnsafeChars("hello"));
            Assert.Throws<IrcException>(() => StringExtensions.CheckUnsafeChars("\n"));
            Assert.Throws<IrcException>(() => StringExtensions.CheckUnsafeChars("\r"));
            Assert.Throws<IrcException>(() => StringExtensions.CheckUnsafeChars("\r\n"));
        }

        [Fact]
        public void TestIsChannel()
        {
            Assert.True(StringExtensions.IsChannel("##Hello"));
            Assert.True(StringExtensions.IsChannel("#Hello"));
            Assert.True(StringExtensions.IsChannel("&Hello"));
            Assert.True(StringExtensions.IsChannel("+Hello"));
            Assert.True(StringExtensions.IsChannel("!Hello"));
            Assert.False(StringExtensions.IsChannel("Hello#"));
            Assert.False(StringExtensions.IsChannel("Hello!"));
            Assert.False(StringExtensions.IsChannel("Hello"));
        }
    }
}
