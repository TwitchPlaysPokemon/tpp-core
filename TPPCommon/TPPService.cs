using System.Threading;
using TPPCommon.Configuration;
using TPPCommon.Logging;
using TPPCommon.PubSub;

namespace TPPCommon
{
    /// <summary>
    /// Base class for all TPP services.
    /// </summary>
    public abstract class TPPService
    {
        protected abstract string[] ConfigNames { get; }
        protected abstract int StartupDelayMilliseconds { get; }

        protected IPublisher Publisher;
        protected ISubscriber Subscriber;
        protected ITPPLoggerFactory LoggerFactory;
        protected IConfigReader ConfigReader;

        public TPPService(IPublisher publisher, ISubscriber subscriber, ITPPLoggerFactory loggerFactory, IConfigReader configReader)
        {
            this.Publisher = publisher;
            this.Subscriber = subscriber;
            this.LoggerFactory = loggerFactory;
            this.ConfigReader = configReader;
        }

        protected abstract void Initialize();
        protected abstract void Run();

        /// <summary>
        /// Initializes and runs the service.
        /// </summary>
        public void RunService()
        {
            this.Initialize();

            Thread.Sleep(this.StartupDelayMilliseconds);
            this.Run();
        }
    }
}
