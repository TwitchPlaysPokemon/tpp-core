using System;
using Xunit;
using TPPCore.Utils;
using System.Linq;

namespace TPPCore.Utils.Tests
{
    public class IrcStringExtensionsTest
    {
        [Fact]
        public void TestToLowerIrc()
        {
            Assert.Equal("abc{}|", IrcStringExtensions.ToLowerIrc("Abc[]\\"));
        }

        [Fact]
        public void TestUnsafeChars()
        {
            Assert.False(IrcStringExtensions.ContainsUnsafeChars("hello"));
            Assert.Throws<Exception>(() => IrcStringExtensions.CheckUnsafeChars("\n"));
            Assert.Throws<Exception>(() => IrcStringExtensions.CheckUnsafeChars("\r"));
            Assert.Throws<Exception>(() => IrcStringExtensions.CheckUnsafeChars("\r\n"));
        }
    }
}
