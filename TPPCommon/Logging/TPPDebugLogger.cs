using System;
using System.Globalization;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TPPCommon.Logging
{
    public class TPPDebugLogger : ITPPLogger
    {
        private string Prefix = string.Empty;
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
        public void LogDebug(string message)
        {
            this.PublishLog(new LogDebugEvent(ApplyPrefix(message)), "DEBUG");
        }

        /// <summary>
        /// Log a general information event.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogInfo(string message)
        {
            this.PublishLog(new LogInfoEvent(ApplyPrefix(message)), "INFO ");
        }

        /// <summary>
        /// Log a warning event.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogWarning(string message)
        {
            this.PublishLog(new LogWarningEvent(ApplyPrefix(message)), "WARN ");
        }

        /// <summary>
        /// Log an error event.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogError(string message)
        {
            this.PublishLog(new LogErrorEvent(ApplyPrefix(message)), "ERROR");
        }

        /// <summary>
        /// Log an error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        public void LogError(string message, Exception e)
        {
            this.PublishLog(new LogErrorExceptionEvent(ApplyPrefix(message), e?.Message, e?.StackTrace), "ERROR");
        }

        /// <summary>
        /// Log a critical error event.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogCritical(string message)
        {
            this.PublishLog(new LogCriticalEvent(ApplyPrefix(message)), "FATAL");
        }

        /// <summary>
        /// Log a critical error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        public void LogCritical(string message, Exception e)
        {
            this.PublishLog(new LogCriticalExceptionEvent(ApplyPrefix(message), e?.Message, e?.StackTrace), "FATAL");
        }
    }
}
