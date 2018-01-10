using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TPPCore.Utils
{
    /// <summary>
    /// Compares two byte arrays in constant time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is for securely comparing bytes or strings by avoiding
    /// timing attacks.
    /// </para>
    /// <para>
    /// A built-in method is not yet in .Net Core so this exists. The
    /// implementation uses Double HMAC Verification.
    /// </remarks>
    public class ConstantTimeComparer
    {
        private readonly byte[] hashedValue;
        private readonly byte[] key;

        public ConstantTimeComparer(byte[] value)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                key = new byte[16];
                rng.GetBytes(key);
            }

            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                hashedValue = hmac.ComputeHash(value);
            }
        }

        public ConstantTimeComparer(string value) : this(Encoding.UTF8.GetBytes(value))
        {
        }

        public bool CheckEquality(byte[] value)
        {
            byte[] targetHashedValue;

            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                targetHashedValue = hmac.ComputeHash(value);
            }

            return hashedValue.SequenceEqual(targetHashedValue);
        }

        public bool CheckEquality(string value)
        {
            return CheckEquality(Encoding.UTF8.GetBytes(value));
        }
    }
}
