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
    public class TPPDebugLogger : TPPLoggerBase
    {
        public TPPDebugLogger(IPublisher publisher, string identifier) : base(publisher, identifier)
        { }

        /// <summary>
        /// Write to the console, in addition to generating a log event.
        /// This helps debugging services individually, rather than lumping them all into one
        /// output.
        /// </summary>
        /// <param name="logEvent">log event</param>
        /// <param name="errorLevel">error level</param>
        private void PublishLog(LogEvent logEvent, string errorLevel, Action<string> writeFunc)
        {
            string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            FormattableString message = $"{dateTime}\t[{errorLevel}]\t{logEvent.Message ?? string.Empty}";
            writeFunc(message.ToString(CultureInfo.InvariantCulture));
            this.Publisher.Publish(logEvent);
        }

        /// <summary>
        /// Log a debug event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogDebug(string message, params object[] args)
        {
            this.PublishLog(new LogDebugEvent(this.NormalizeMessage(message, args)), "DEBUG", Console.WriteLine);
        }

        /// <summary>
        /// Log a general information event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogInfo(string message, params object[] args)
        {
            this.PublishLog(new LogInfoEvent(this.NormalizeMessage(message, args)), "INFO ", Console.WriteLine);
        }

        /// <summary>
        /// Log a warning event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogWarning(string message, params object[] args)
        {
            this.PublishLog(new LogWarningEvent(this.NormalizeMessage(message, args)), "WARN ", Console.Error.WriteLine);
        }

        /// <summary>
        /// Log an error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogError(string message, params object[] args)
        {
            this.PublishLog(new LogErrorEvent(this.NormalizeMessage(message, args)), "ERROR", Console.Error.WriteLine);
        }

        /// <summary>
        /// Log an error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public override void LogError(string message, Exception e, params object[] args)
        {
            this.PublishLog(new LogErrorExceptionEvent(this.NormalizeMessage(message, args), e?.Message, e?.StackTrace), "ERROR", Console.Error.WriteLine);
        }

        /// <summary>
        /// Log a critical error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogCritical(string message, params object[] args)
        {
            this.PublishLog(new LogCriticalEvent(this.NormalizeMessage(message, args)), "FATAL", Console.Error.WriteLine);
        }

        /// <summary>
        /// Log a critical error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public override void LogCritical(string message, Exception e, params object[] args)
        {
            this.PublishLog(new LogCriticalExceptionEvent(this.NormalizeMessage(message, args), e?.Message, e?.StackTrace), "FATAL", Console.Error.WriteLine);
        }
    }
}
