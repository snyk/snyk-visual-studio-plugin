using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Globalization;

namespace Snyk.VisualStudio.Extension
{
    public class SnykActivityLogger
    {
        private const string LogMessageTemplate = "Snyk Activity Log: {0}";

        private IVsActivityLog activityLog;

        public SnykActivityLogger(IVsActivityLog activityLog)
        {
            this.activityLog = activityLog;
        }

        public virtual void LogInformation(string message) =>
            activityLog.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
                this.ToString(),
                string.Format(CultureInfo.CurrentCulture,
                LogMessageTemplate, message));

        public virtual void LogError(string message) =>
            activityLog.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                this.ToString(),
                string.Format(CultureInfo.CurrentCulture,
                LogMessageTemplate, message));
    }    
}
