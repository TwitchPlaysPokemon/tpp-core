using System;
using Xunit;
using TPPCommon;

namespace TPPCommonTest
{
    /// <summary>
    /// Test functionality of the Addresses class.
    /// </summary>
    public class AddressesTest
    {
        [Fact]
        public void Addresses_BuildFullAddress_ValidArgs_Success()
        {
            string expected = "tcp://127.0.0.1:1337";
            string result = Addresses.BuildFullAddress("tcp://127.0.0.1", 1337);
            Assert.Equal(expected, result);

            expected = "tcp://localhost:0";
            result = Addresses.BuildFullAddress("tcp://localhost", 0);
            Assert.Equal(expected, result);

            expected = "tcp://127.0.0.1:65535";
            result = Addresses.BuildFullAddress("tcp://127.0.0.1", 65535);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Addresses_BuildFullAddress_EmptyBaseAddress_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Addresses.BuildFullAddress("", 1337));
        }

        [Fact]
        public void Addresses_BuildFullAddress_NullBaseAddress_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Addresses.BuildFullAddress(null, 1337));
        }

        [Fact]
        public void Addresses_BuildFullAddress_NegativePort_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Addresses.BuildFullAddress("tcp://127.0.0.1", -1));
        }

        [Fact]
        public void Addresses_BuildFullAddress_TooHighPort_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Addresses.BuildFullAddress("tcp://127.0.0.1", 65536));
        }
    }
}
