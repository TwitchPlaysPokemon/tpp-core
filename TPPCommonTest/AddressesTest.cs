using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TPPCommon;

namespace TPPCommonTest
{
    /// <summary>
    /// Test functionality of the Addresses class.
    /// </summary>
    [TestClass]
    public class AddressesTest
    {
        [TestMethod]
        public void Addresses_BuildFullAddress_ValidArgs_Success()
        {
            string expected = "tcp://127.0.0.1:1337";
            string result = Addresses.BuildFullAddress("tcp://127.0.0.1", 1337);
            Assert.AreEqual(expected, result);

            expected = "tcp://localhost:0";
            result = Addresses.BuildFullAddress("tcp://localhost", 0);
            Assert.AreEqual(expected, result);

            expected = "tcp://127.0.0.1:65535";
            result = Addresses.BuildFullAddress("tcp://127.0.0.1", 65535);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Addresses_BuildFullAddress_EmptyBaseAddress_Throws()
        {
            string result = Addresses.BuildFullAddress("", 1337);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Addresses_BuildFullAddress_NullBaseAddress_Throws()
        {
            string result = Addresses.BuildFullAddress(null, 1337);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Addresses_BuildFullAddress_NegativePort_Throws()
        {
            string result = Addresses.BuildFullAddress("tcp://127.0.0.1", -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Addresses_BuildFullAddress_TooHighPort_Throws()
        {
            string result = Addresses.BuildFullAddress("tcp://127.0.0.1", 65536);
        }
    }
}
