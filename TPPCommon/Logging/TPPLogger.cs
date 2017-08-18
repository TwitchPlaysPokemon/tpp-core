using System;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TPPCommon.Logging
{
    /// <summary>
    /// Class which TPP services directly use to perform logging.
    /// This class isn't responsible for perform the actual logging, rather it publishes logging
    /// events, to which a logging service can listen.
    /// </summary>
    public class TPPLogger
    {
        private string Prefix = string.Empty;
        private IPublisher Publisher;

        public TPPLogger(IPublisher publisher)
        {
            this.Publisher = publisher;
        }

        /// <summary>
        /// Specifies a prefix to add to all log messages.
        /// </summary>
        /// <param name="prefixMessage">log message prefix</param>
        public void SetLogPrefix(string prefixMessage)
        {
            this.Prefix = prefixMessage ?? string.Empty;
        }

        private string ApplyPrefix(string message)
        {
            return this.Prefix + message;
        }

        /// <summary>
        /// Log a debug event.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogDebug(string message)
        {
            this.Publisher.Publish(new LogDebugEvent(ApplyPrefix(message)));
        }

        /// <summary>
        /// Log a general information event.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogInfo(string message)
        {
            this.Publisher.Publish(new LogInfoEvent(ApplyPrefix(message)));
        }

        /// <summary>
        /// Log a warning event.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogWarning(string message)
        {
            this.Publisher.Publish(new LogWarningEvent(ApplyPrefix(message)));
        }

        /// <summary>
        /// Log an error event.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogError(string message)
        {
            this.Publisher.Publish(new LogErrorEvent(ApplyPrefix(message)));
        }

        /// <summary>
        /// Log an error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        public void LogError(string message, Exception e)
        {
            this.Publisher.Publish(new LogErrorExceptionEvent(ApplyPrefix(message), e?.Message, e?.StackTrace));
        }

        /// <summary>
        /// Log a critical error event.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogCritical(string message)
        {
            this.Publisher.Publish(new LogCriticalEvent(ApplyPrefix(message)));
        }

        /// <summary>
        /// Log a critical error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        public void LogCritical(string message, Exception e)
        {
            this.Publisher.Publish(new LogCriticalExceptionEvent(ApplyPrefix(message), e?.Message, e?.StackTrace));
        }
    }
}
