namespace TPPCommon.Logging
{
    public interface ITPPLoggerFactory
    {
        /// <summary>
        /// Creates an instance of a logger with the given identifier. This identifier will be
        /// included in all logged messages.
        /// </summary>
        /// <param name="identifier">log identifier</param>
        /// <returns>logger instance</returns>
        TPPLoggerBase Create(string identifier);
    }
}
