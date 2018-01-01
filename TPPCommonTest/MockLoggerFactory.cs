using TPPCommon.Logging;
using TPPCommon.PubSub;
using Xunit.Abstractions;

namespace TPPCommonTest
{
    public class MockLoggerFactory : ITPPLoggerFactory
    {
        protected IPublisher Publisher;
        protected ITestOutputHelper Output;

        public MockLoggerFactory(IPublisher publisher, ITestOutputHelper output)
        {
            this.Publisher = publisher;
            this.Output = output;
        }

        public TPPLoggerBase Create(string identifier)
        {
            return new MockLogger(Publisher, identifier, Output);
        }
    }
}
