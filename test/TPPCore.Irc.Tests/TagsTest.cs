using System;
using TPPCore.Irc;
using Xunit;

namespace TPPCore.Utils.Tests
{
    public class TagsTest
    {
        [Fact]
        public void TestParse()
        {
            var tags = new Tags();
            tags.ParseFrom(@"aaa=bbb;ccc;example.com/ddd=eee;fff=hello\sworld!");

            Assert.Equal(4, tags.Count);
            Assert.Equal("hello world!", tags["fff"]);
        }

        [Fact]
        public void TestToString()
        {
            var tags = new Tags();
            tags["aaa"] = "bbb";
            tags["ccc"] = "";
            tags["example.com/ddd"] = "eee";
            tags["fff"] = "hello world!";

            Assert.Equal(
                @"aaa=bbb;ccc;example.com/ddd=eee;fff=hello\sworld!",
                tags.ToString());
        }

        [Fact]
        public void TestEscape()
        {
            Assert.Equal("; \\\r\n", Tags.Unescape(@"\:\s\\\r\n"));
            Assert.Equal(@"\:\s\\\r\n", Tags.Escape("; \\\r\n"));
        }
    }
}
