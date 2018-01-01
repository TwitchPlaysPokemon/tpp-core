using TPPCommon.PubSub;

namespace TPPCommon.Logging
{
    /// <summary>
    /// Factory responsible for creating <see cref="TPPLogger"/> instances.
    /// </summary>
    public class TPPLoggerFactory : ITPPLoggerFactory
    {
        private IPublisher Publisher;

        public TPPLoggerFactory(IPublisher publisher)
        {
            this.Publisher = publisher;
        }

        /// <summary>
        /// Creates an instance of a <see cref="TPPLogger"/> with the given identifier. This identifier will be
        /// included in all logged messages.
        /// </summary>
        /// <param name="identifier">log identifier</param>
        /// <returns>logger instance</returns>
        public TPPLoggerBase Create(string identifier)
        {
            return new TPPLogger(this.Publisher, identifier);
        }
    }
}
