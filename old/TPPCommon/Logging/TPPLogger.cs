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
    internal class TPPLogger : TPPLoggerBase
    {
        public TPPLogger(IPublisher publisher, string identifier) : base(publisher, identifier)
        { }

        /// <summary>
        /// Log a debug event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogDebug(string message, params object[] args)
        {
            this.Publisher.Publish(new LogDebugEvent(this.NormalizeMessage(message, args)));
        }

        /// <summary>
        /// Log a general information event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogInfo(string message, params object[] args)
        {
            this.Publisher.Publish(new LogInfoEvent(this.NormalizeMessage(message, args)));
        }

        /// <summary>
        /// Log a warning event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogWarning(string message, params object[] args)
        {
            this.Publisher.Publish(new LogWarningEvent(this.NormalizeMessage(message, args)));
        }

        /// <summary>
        /// Log an error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogError(string message, params object[] args)
        {
            this.Publisher.Publish(new LogErrorEvent(this.NormalizeMessage(message, args)));
        }

        /// <summary>
        /// Log an error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public override void LogError(string message, Exception e, params object[] args)
        {
            this.Publisher.Publish(new LogErrorExceptionEvent(this.NormalizeMessage(message, args), e?.Message, e?.StackTrace));
        }

        /// <summary>
        /// Log a critical error event.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="args">string format args</param>
        public override void LogCritical(string message, params object[] args)
        {
            this.Publisher.Publish(new LogCriticalEvent(this.NormalizeMessage(message, args)));
        }

        /// <summary>
        /// Log a critical error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        /// <param name="args">string format args</param>
        public override void LogCritical(string message, Exception e, params object[] args)
        {
            this.Publisher.Publish(new LogCriticalExceptionEvent(this.NormalizeMessage(message, args), e?.Message, e?.StackTrace));
        }
    }
}
