using TPPCommon.PubSub;

namespace TPPCommon.Logging
{
    /// <summary>
    /// Factory responsible for creating <see cref="TPPDebugLogger"/> instances.
    /// </summary>
    public class TPPDebugLoggerFactory : ITPPLoggerFactory
    {
        private IPublisher Publisher;

        public TPPDebugLoggerFactory(IPublisher publisher)
        {
            this.Publisher = publisher;
        }

        /// <summary>
        /// Creates an instance of a <see cref="TPPDebugLogger"/> with the given identifier. This identifier will be
        /// included in all logged messages.
        /// </summary>
        /// <param name="identifier">log identifier</param>
        /// <returns>logger instance</returns>
        public TPPLoggerBase Create(string identifier)
        {
            return new TPPDebugLogger(this.Publisher, identifier);
        }
    }
}
