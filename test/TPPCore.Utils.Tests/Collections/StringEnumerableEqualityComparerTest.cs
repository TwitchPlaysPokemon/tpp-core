using System;
using Xunit;
using TPPCore.Utils.Collections;

namespace TPPCore.Utils.Tests.Collections
{
    public class StringEnumerableEqualityComparerTest
    {
        [Fact]
        public void TestCompare()
        {
            var comparer = new StringEnumerableEqualityComparer<string[]>();

            var arrayAB1 = new string[] {"a", "b"};
            var arrayAB2 = new string[] {"a", "b"};
            var arrayXY1 = new string[] {"x", "y"};

            Assert.True(comparer.Equals(arrayAB1, arrayAB2));
            Assert.False(comparer.Equals(arrayAB1, arrayXY1));
        }
    }
}
