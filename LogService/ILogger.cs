using System;

namespace LogService
{
    /// <summary>
    /// Interface for logging messages.
    /// </summary>
    internal interface ILogger
    {
        /// <summary>
        /// Log a debug message.
        /// </summary>
        /// <param name="message">log message</param>
        void LogDebug(string message);

        /// <summary>
        /// Log a general information message.
        /// </summary>
        /// <param name="message">log message</param>
        void LogInfo(string message);

        /// <summary>
        /// Log a warning message.
        /// </summary>
        /// <param name="message">log message</param>
        void LogWarning(string message);

        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="message">log message</param>
        void LogError(string message);

        /// <summary>
        /// Log an error message along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        void LogError(string message, Exception e);

        /// <summary>
        /// Log a critical error message.
        /// </summary>
        /// <param name="message">log message</param>
        void LogCritical(string message);

        /// <summary>
        /// Log a critical error message along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="e">exception</param>
        void LogCritical(string message, Exception e);
    }
}
