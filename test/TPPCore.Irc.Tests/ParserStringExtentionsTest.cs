using System;
using Xunit;
using TPPCore.Irc;

namespace TPPCore.Utils.Tests
{
    public class ParserStringExtensionsTest
    {
        [Fact]
        public void TestSplitSpace()
        {
            var result = ParserStringExtensions.SplitSpace("abc");
            Assert.Equal("abc", result.First);
            Assert.Equal("", result.Remainder);

            result = ParserStringExtensions.SplitSpace("abc def");
            Assert.Equal("abc", result.First);
            Assert.Equal("def", result.Remainder);

            result = ParserStringExtensions.SplitSpace("abc  def ghi  jkl");
            Assert.Equal("abc", result.First);
            Assert.Equal("def ghi  jkl", result.Remainder);
        }
    }
}
