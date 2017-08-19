using System;
using System.Collections.Generic;
using System.Text;

namespace TPPCommon.Logging
{
    public interface ITPPLogger
    {
        /// <summary>
        /// Specifies a prefix to add to all log messages.
        /// </summary>
        /// <param name="prefixMessage">log message prefix</param>
        void SetLogPrefix(string prefixMessage);

        /// <summary>
        /// Log a debug event.
        /// </summary>
        /// <param name="message">log message</param>
        void LogDebug(string message);

        /// <summary>
        /// Log a general information event.
        /// </summary>
        /// <param name="message">log message</param>
        void LogInfo(string message);

        /// <summary>
        /// Log a warning event.
        /// </summary>
        /// <param name="message">log message</param>
        void LogWarning(string message);

        /// <summary>
        /// Log an error event.
        /// </summary>
        /// <param name="message">log message</param>
        void LogError(string message);

        /// <summary>
        /// Log an error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        void LogError(string message, Exception e);

        /// <summary>
        /// Log a critical error event.
        /// </summary>
        /// <param name="message">log message</param>
        void LogCritical(string message);

        /// <summary>
        /// Log a critical error event along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        void LogCritical(string message, Exception e);
    }
}
