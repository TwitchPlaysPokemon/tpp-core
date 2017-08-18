using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace TPPCommon.PubSub.Events
{
    /// <summary>
    /// Pub-sub event class for debug-level log messages.
    /// </summary>
    [DataContract]
    [Topic("log_debug")]
    public class LogDebugEvent : PubSubEvent
    {
        [DataMember]
        public string Message { get; set; }

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
    public class LogInfoEvent : PubSubEvent
    {
        [DataMember]
        public string Message { get; set; }

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
    public class LogWarningEvent : PubSubEvent
    {
        [DataMember]
        public string Message { get; set; }

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
    public class LogErrorEvent : PubSubEvent
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public Exception @Exception { get; set; }

        public LogErrorEvent(string message) : this(message, null)
        { }

        public LogErrorEvent(string message, Exception e)
        {
            this.Message = message;
            this.Exception = e;
        }
    }

    /// <summary>
    /// Pub-sub event class for error-level log messages.
    /// </summary>
    [DataContract]
    [Topic("log_critical")]
    public class LogCriticalEvent : PubSubEvent
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public Exception @Exception { get; set; }

        public LogCriticalEvent(string message) : this(message, null)
        { }

        public LogCriticalEvent(string message, Exception e)
        {
            this.Message = message;
            this.Exception = e;
        }
    }
}
