using System;

namespace TPPCommon.PubSub
{
    public class InvalidTopicException: ArgumentException
    {
        public InvalidTopicException(string message, string paramName) : base(message, paramName)
        { }
    }
}
