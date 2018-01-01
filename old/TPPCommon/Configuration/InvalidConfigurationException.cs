using System;

namespace TPPCommon.Configuration
{
    internal class InvalidConfigurationException : Exception
    {
        public InvalidConfigurationException(string message) : base(message)
        { }
    }
}
