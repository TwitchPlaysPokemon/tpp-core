using System;
using Xunit;
using TPPCore.Utils;
using System.Linq;

namespace TPPCore.Utils.Tests
{
    public class ConstantTimeComparerTest
    {
        [Fact]
        public void TestComparer()
        {
            var equals = new ConstantTimeComparer("a");

            foreach (var index in Enumerable.Range(0, 10))
            {
                Assert.True(equals.CheckEquality("a"));
                Assert.False(equals.CheckEquality("b"));
                Assert.False(equals.CheckEquality(""));
            }
        }
    }
}
