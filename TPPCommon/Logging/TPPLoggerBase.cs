using System.Globalization;

namespace TPPCommon.Logging
{
    /// <summary>
    /// Base class for TPPLoggers. Handles localization normalization.
    /// </summary>
    public abstract class TPPLoggerBase
    {
        protected string Prefix = string.Empty;

        protected string NormalizeMessage(string message, params object[] args)
        {
            return this.Prefix + string.Format(CultureInfo.InvariantCulture, message, args);
        }
    }
}
