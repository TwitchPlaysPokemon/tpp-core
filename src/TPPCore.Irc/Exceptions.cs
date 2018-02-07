using System;

namespace TPPCore.Irc
{
    public class IrcException : Exception
    {
        public IrcException() {}
        public IrcException(string message) : base(message) {}
        public IrcException(string message, Exception innerException)
            : base(message, innerException) {}
    }

    public class IrcConnectionException : IrcException
    {
        public IrcConnectionException() {}
        public IrcConnectionException(string message) : base(message) {}
        public IrcConnectionException(string message, Exception innerException)
            : base(message, innerException) {}
    }

    public class IrcParserException : IrcException
    {
        public IrcParserException() {}
        public IrcParserException(string message) : base(message) {}
        public IrcParserException(string message, Exception innerException)
            : base(message, innerException) {}
    }

    public class IrcTimeoutException : Exception
    {
        public IrcTimeoutException() {}
        public IrcTimeoutException(string message) : base(message) {}
        public IrcTimeoutException(string message, Exception innerException)
            : base(message, innerException) {}
    }
}
