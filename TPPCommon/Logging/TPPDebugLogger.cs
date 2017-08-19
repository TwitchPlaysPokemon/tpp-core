using System;
using System.Globalization;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TPPCommon.Logging
{
    /// <summary>
    /// Class which TPP services directly use to perform logging.
    /// This class isn't responsible for perform the actual logging, rather it publishes logging
    /// events, to which a logging service can listen.
    /// 
    /// This is meant to be used during local development, because it writes logs directly to the console, in addition to
    /// publishing log events.
    /// </summary>
    public class TPPDebugLogger : TPPLoggerBase, ITPPLogger
    {
        private IPublisher Publisher;

        public TPPDebugLogger(IPublisher publisher)
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
        /// Write to the console, in addition to generating a log event.
        /// This helps debugging services individually, rather than lumping them all into one
        /// output.
        /// </summary>
        /// <param name="logEvent">log event</param>
        /// <param name="errorLevel">error level</param>
        private void PublishLog(LogEvent logEvent, string errorLevel)
        {
            string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            FormattableString message = $"{dateTime} [{errorLevel}] {logEvent.Message ?? string.Empty}";
            Console.WriteLine(message.ToString(CultureInfo.InvariantCulture));
            this.Publisher.Publish(logEvent);
        }

        /// <summary>
        /// Log a debug event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public void LogDebug(string message, params object[] args)
        {
            this.PublishLog(new LogDebugEvent(this.NormalizeMessage(message, args)), "DEBUG");
        }

        /// <summary>
        /// Log a general information event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public void LogInfo(string message, params object[] args)
        {
            this.PublishLog(new LogInfoEvent(this.NormalizeMessage(message, args)), "INFO ");
        }

        /// <summary>
        /// Log a warning event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public void LogWarning(string message, params object[] args)
        {
            this.PublishLog(new LogWarningEvent(this.NormalizeMessage(message, args)), "WARN ");
        }

        /// <summary>
        /// Log an error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public void LogError(string message, params object[] args)
        {
            this.PublishLog(new LogErrorEvent(this.NormalizeMessage(message, args)), "ERROR");
        }

        /// <summary>
        /// Log an error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public void LogError(string message, Exception e, params object[] args)
        {
            this.PublishLog(new LogErrorExceptionEvent(this.NormalizeMessage(message, args), e?.Message, e?.StackTrace), "ERROR");
        }

        /// <summary>
        /// Log a critical error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public void LogCritical(string message, params object[] args)
        {
            this.PublishLog(new LogCriticalEvent(this.NormalizeMessage(message, args)), "FATAL");
        }

        /// <summary>
        /// Log a critical error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public void LogCritical(string message, Exception e, params object[] args)
        {
            this.PublishLog(new LogCriticalExceptionEvent(this.NormalizeMessage(message, args), e?.Message, e?.StackTrace), "FATAL");
        }
    }
}
