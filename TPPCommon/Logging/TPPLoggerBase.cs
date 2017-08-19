using System;
using System.Globalization;
using TPPCommon.PubSub;

namespace TPPCommon.Logging
{
    /// <summary>
    /// Base class for TPPLoggers.
    /// </summary>
    public abstract class TPPLoggerBase
    {
        protected IPublisher Publisher;
        protected string Identifier = string.Empty;

        public TPPLoggerBase(IPublisher publisher, string identifier)
        {
            this.Publisher = publisher;
            this.Identifier = identifier;
        }

        protected string NormalizeMessage(string message, params object[] args)
        {
            return $"[{this.Identifier}]\t{string.Format(CultureInfo.InvariantCulture, message, args)}";
        }

        /// <summary>
        /// Log a debug event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public abstract void LogDebug(string message, params object[] args);

        /// <summary>
        /// Log a general information event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public abstract void LogInfo(string message, params object[] args);

        /// <summary>
        /// Log a warning event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public abstract void LogWarning(string message, params object[] args);

        /// <summary>
        /// Log an error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public abstract void LogError(string message, params object[] args);

        /// <summary>
        /// Log an error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public abstract void LogError(string message, Exception e, params object[] args);

        /// <summary>
        /// Log a critical error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public abstract void LogCritical(string message, params object[] args);

        /// <summary>
        /// Log a critical error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public abstract void LogCritical(string message, Exception e, params object[] args);
    }
}
