using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace LogService
{
    internal class Log4NetLogger : ILogger
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Log4NetLogger));
        private string Prefix = string.Empty;

        static Log4NetLogger()
        {
            // Manually load the log4net configuration file.
            XmlDocument config = new XmlDocument();

            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string configPath = Path.Combine(assemblyDirectory, "log4net.config");
            config.Load(File.OpenRead(configPath));

            var repository = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(Hierarchy));
            XmlConfigurator.Configure(repository, config["log4net"]);
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
        /// Log a debug message.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogDebug(string message)
        {
            Log4NetLogger.Log.Debug(ApplyPrefix(message));
        }

        /// <summary>
        /// Log a general information message.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogInfo(string message)
        {
            Log4NetLogger.Log.Info(ApplyPrefix(message));
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogWarning(string message)
        {
            Log4NetLogger.Log.Warn(ApplyPrefix(message));
        }

        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogError(string message)
        {
            Log4NetLogger.Log.Error(ApplyPrefix(message));
        }

        /// <summary>
        /// Log an error message along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="exceptionMessage">exception message</param>
        /// <param name="stackTrace">exception stack trace</param>
        public void LogError(string message, string exceptionMessage, string stackTrace)
        {
            Log4NetLogger.Log.Error($"{ApplyPrefix(message)}{Environment.NewLine}{exceptionMessage}{Environment.NewLine}{stackTrace}");
        }

        /// <summary>
        /// Log a critical error message.
        /// </summary>
        /// <param name="message">log message</param>
        public void LogCritical(string message)
        {
            Log4NetLogger.Log.Fatal(ApplyPrefix(message));
        }

        /// <summary>
        /// Log a critical error message along with its exception.
        /// </summary>
        /// <param name="message">log message</param>
        /// <param name="exceptionMessage">exception message</param>
        /// <param name="stackTrace">exception stack trace</param>
        public void LogCritical(string message, string exceptionMessage, string stackTrace)
        {
            Log4NetLogger.Log.Fatal($"{ApplyPrefix(message)}{Environment.NewLine}{exceptionMessage}{Environment.NewLine}{stackTrace}");
        }
    }
}
