namespace Snyk.VisualStudio.Extension
{
    using System;
    using System.Globalization;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Wrapper for Visual Studio logger.
    /// </summary>
    public class SnykActivityLogger
    {
        private const string LogMessageTemplate = "Snyk Activity Log: {0}";

        private IVsActivityLog activityLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykActivityLogger"/> class.
        /// </summary>
        /// <param name="activityLog">Visual Studio logger instance.</param>
        public SnykActivityLogger(IVsActivityLog activityLog) => this.activityLog = activityLog;

        /// <summary>
        /// Log information message.
        /// </summary>
        /// <param name="message">Message string.</param>
        public virtual void LogInformation(string message) =>
            this.activityLog.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
                this.ToString(),
                string.Format(CultureInfo.CurrentCulture,
                LogMessageTemplate, message));

        /// <summary>
        /// Log error message.
        /// </summary>
        /// <param name="message">Message string.</param>
        public virtual void LogError(string message) =>
            this.activityLog.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                this.ToString(),
                string.Format(CultureInfo.CurrentCulture,
                LogMessageTemplate, message));
    }
}