using System;
using TPPCommon.Logging;
using TPPCommon.PubSub;
using Xunit.Abstractions;

namespace TPPCommonTest
{
    public class MockLogger : TPPLoggerBase
    {
        protected readonly ITestOutputHelper Output;

        public MockLogger(IPublisher publisher, string identifier,
                ITestOutputHelper output) : base(publisher, identifier)
        {
            this.Output = output;
        }

        public override void LogCritical(string message, params object[] args)
        {
            Output.WriteLine(message + args);
        }

        public override void LogCritical(string message, Exception e, params object[] args)
        {
            Output.WriteLine(message + e + args);
        }

        public override void LogDebug(string message, params object[] args)
        {
            Output.WriteLine(message + args);
        }

        public override void LogError(string message, params object[] args)
        {
            Output.WriteLine(message + args);
        }

        public override void LogError(string message, Exception e, params object[] args)
        {
            Output.WriteLine(message + e + args);
        }

        public override void LogInfo(string message, params object[] args)
        {
            Output.WriteLine(message + args);
        }

        public override void LogWarning(string message, params object[] args)
        {
            Output.WriteLine(message + args);
        }
    }
}
