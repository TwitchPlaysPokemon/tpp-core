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
    internal class TPPDebugLogger : TPPLoggerBase
    {
        public TPPDebugLogger(IPublisher publisher, string identifier) : base(publisher, identifier)
        { }

        /// <summary>
        /// Write to the console, in addition to generating a log event.
        /// This helps debugging services individually, rather than lumping them all into one
        /// output.
        /// </summary>
        /// <param name="logEvent">log event</param>
        /// <param name="message">message to log locally</param>
        /// <param name="errorLevel">error level</param>
        /// <param name="stderr">whether or not to write to standard error. Otherwise, it will use standard out.</param>
        private void PublishLog(LogEvent logEvent, string message, string errorLevel, bool stderr = false)
        {
            // Write message to local console before publishing log event.
            string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            FormattableString formattableMessage = $"{dateTime}\t[{errorLevel}]\t{message ?? string.Empty}";
            string formattedMessage = formattableMessage.ToString(CultureInfo.InvariantCulture);
            if (stderr)
            {
                Console.Error.WriteLine(formattedMessage);
            }
            else
            {
                Console.WriteLine(formattedMessage);
            }

            this.Publisher.Publish(logEvent);
        }

        private string NormalizeExceptionMessage(string message, string exceptionMessage, string stackTrace)
        {
            return $"{message}{Environment.NewLine}{exceptionMessage}{Environment.NewLine}{stackTrace}";
        }

        /// <summary>
        /// Log a debug event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogDebug(string message, params object[] args)
        {
            LogDebugEvent logEvent = new LogDebugEvent(this.NormalizeMessage(message, args));
            this.PublishLog(logEvent, logEvent.Message, "DEBUG");
        }

        /// <summary>
        /// Log a general information event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogInfo(string message, params object[] args)
        {
            LogInfoEvent logEvent = new LogInfoEvent(this.NormalizeMessage(message, args));
            this.PublishLog(logEvent, logEvent.Message, "INFO ");
        }

        /// <summary>
        /// Log a warning event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogWarning(string message, params object[] args)
        {
            LogWarningEvent logEvent = new LogWarningEvent(this.NormalizeMessage(message, args));
            this.PublishLog(logEvent, logEvent.Message, "WARN ", stderr: true);
        }

        /// <summary>
        /// Log an error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogError(string message, params object[] args)
        {
            LogErrorEvent logEvent = new LogErrorEvent(this.NormalizeMessage(message, args));
            this.PublishLog(logEvent, logEvent.Message, "ERROR", stderr: true);
        }

        /// <summary>
        /// Log an error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public override void LogError(string message, Exception e, params object[] args)
        {
            LogErrorExceptionEvent logEvent = new LogErrorExceptionEvent(this.NormalizeMessage(message, args), e?.Message, e?.StackTrace);
            string exceptionMessage = this.NormalizeExceptionMessage(logEvent.Message, e?.Message, e?.StackTrace);
            this.PublishLog(logEvent, exceptionMessage, "ERROR", stderr: true);
        }

        /// <summary>
        /// Log a critical error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogCritical(string message, params object[] args)
        {
            LogCriticalEvent logEvent = new LogCriticalEvent(this.NormalizeMessage(message, args));
            this.PublishLog(logEvent, logEvent.Message, "FATAL", stderr: true);
        }

        /// <summary>
        /// Log a critical error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public override void LogCritical(string message, Exception e, params object[] args)
        {
            LogCriticalExceptionEvent logEvent = new LogCriticalExceptionEvent(this.NormalizeMessage(message, args), e?.Message, e?.StackTrace);
            string exceptionMessage = this.NormalizeExceptionMessage(logEvent.Message, e?.Message, e?.StackTrace);
            this.PublishLog(logEvent, exceptionMessage, "FATAL", stderr: true);
        }
    }
}
