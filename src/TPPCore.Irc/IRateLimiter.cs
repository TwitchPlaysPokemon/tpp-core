namespace TPPCore.Irc
{
    /// <summary>
    /// Limits how fast messages should be sent.
    /// </summary>
    public interface IRateLimiter
    {
        /// <summary>
        /// Returns time in milliseconds before sending.
        /// </summary>
        int GetWaitTime();

        /// <summary>
        /// Notify that a message was sent.
        /// </summary>
        void Increment();
    }
}
