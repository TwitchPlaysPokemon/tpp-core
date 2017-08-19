using System.Runtime.Serialization;

namespace TPPCommon.PubSub.Events
{
    [DataContract]
    [Topic("log")]
    public class LogEvent : PubSubEvent
    {
        [DataMember]
        public string Message { get; set; }
    }

    /// <summary>
    /// Pub-sub event class for debug-level log messages.
    /// </summary>
    [DataContract]
    [Topic("log_debug")]
    public class LogDebugEvent : LogEvent
    {
        public LogDebugEvent(string message)
        {
            this.Message = message;
        }
    }

    /// <summary>
    /// Pub-sub event class for info-level log messages.
    /// </summary>
    [DataContract]
    [Topic("log_info")]
    public class LogInfoEvent : LogEvent
    {
        public LogInfoEvent(string message)
        {
            this.Message = message;
        }
    }

    /// <summary>
    /// Pub-sub event class for warning-level log messages.
    /// </summary>
    [DataContract]
    [Topic("log_warning")]
    public class LogWarningEvent : LogEvent
    {
        public LogWarningEvent(string message)
        {
            this.Message = message;
        }
    }

    /// <summary>
    /// Pub-sub event class for error-level log messages.
    /// </summary>
    [DataContract]
    [Topic("log_error")]
    public class LogErrorEvent : LogEvent
    {
        public LogErrorEvent(string message)
        {
            this.Message = message;
        }
    }

    /// <summary>
    /// Pub-sub event class for error-level log messages.
    /// </summary>
    [DataContract]
    [Topic("log_error_exception")]
    public class LogErrorExceptionEvent : LogEvent
    {
        [DataMember]
        public string ExceptionMessage { get; set; }

        [DataMember]
        public string StackTrace { get; set; }

        public LogErrorExceptionEvent(string message, string exceptionMessage, string stackTrace)
        {
            this.Message = message;
            this.ExceptionMessage = exceptionMessage;
            this.StackTrace = stackTrace;
        }
    }

    /// <summary>
    /// Pub-sub event class for critical-level log messages.
    /// </summary>
    [DataContract]
    [Topic("log_critical")]
    public class LogCriticalEvent : LogEvent
    {
        public LogCriticalEvent(string message)
        {
            this.Message = message;
        }
    }

    /// <summary>
    /// Pub-sub event class for critical-level log messages.
    /// </summary>
    [DataContract]
    [Topic("log_critical_exception")]
    public class LogCriticalExceptionEvent : LogEvent
    {
        [DataMember]
        public string ExceptionMessage { get; set; }

        [DataMember]
        public string StackTrace { get; set; }

        public LogCriticalExceptionEvent(string message, string exceptionMessage, string stackTrace)
        {
            this.Message = message;
            this.ExceptionMessage = exceptionMessage;
            this.StackTrace = stackTrace;
        }
    }
}
