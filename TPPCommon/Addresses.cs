﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TPPCommon
{
    /// <summary>
    /// Helper class to contain logic for network addressing.
    /// </summary>
    public class Addresses
    {
        /// <summary>
        /// The port used for pub-sub communication. TODO: This assumes only a single port is used for all pub-sub messages.
        /// </summary>
        public const int PubSubPort = 1337;

        /// <summary>
        /// Base network address for localhost.
        /// </summary>
        public const string TCPLocalHost = @"tcp://localhost";

        /// <summary>
        /// Construct a full network address from a base address and port number.
        /// 
        /// Example: tcp://127.0.0.1:1337
        /// </summary>
        /// <param name="baseAddress">base network address</param>
        /// <param name="port">port number</param>
        /// <returns>full network address</returns>
        public static string BuildFullAddress(string baseAddress, int port)
        {
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                throw new ArgumentNullException(nameof(baseAddress));
            }

            if (port < 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), $"Port number must be between 0-65535, but is {port}");
            }

            return $"{baseAddress}:{port}";
        }
    }
}
