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
    public class TPPLogger : TPPLoggerBase, ITPPLogger
    {
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

        /// <summary>
        /// Log a debug event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public void LogDebug(string message, params object[] args)
        {
            this.Publisher.Publish(new LogDebugEvent(this.NormalizeMessage(message, args)));
        }

        /// <summary>
        /// Log a general information event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public void LogInfo(string message, params object[] args)
        {
            this.Publisher.Publish(new LogInfoEvent(this.NormalizeMessage(message, args)));
        }

        /// <summary>
        /// Log a warning event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public void LogWarning(string message, params object[] args)
        {
            this.Publisher.Publish(new LogWarningEvent(this.NormalizeMessage(message, args)));
        }

        /// <summary>
        /// Log an error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public void LogError(string message, params object[] args)
        {
            this.Publisher.Publish(new LogErrorEvent(this.NormalizeMessage(message, args)));
        }

        /// <summary>
        /// Log an error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public void LogError(string message, Exception e, params object[] args)
        {
            this.Publisher.Publish(new LogErrorExceptionEvent(this.NormalizeMessage(message, args), e?.Message, e?.StackTrace));
        }

        /// <summary>
        /// Log a critical error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public void LogCritical(string message, params object[] args)
        {
            this.Publisher.Publish(new LogCriticalEvent(this.NormalizeMessage(message, args)));
        }

        /// <summary>
        /// Log a critical error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public void LogCritical(string message, Exception e, params object[] args)
        {
            this.Publisher.Publish(new LogCriticalExceptionEvent(this.NormalizeMessage(message, args), e?.Message, e?.StackTrace));
        }
    }
}
