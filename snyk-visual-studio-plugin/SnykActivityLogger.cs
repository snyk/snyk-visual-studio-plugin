using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Globalization;

namespace Snyk.VisualStudio.Extension
{
    public class SnykActivityLogger
    {
        private const string LogMessageTemplate = "Snyk Activity Log: {0}";

        private readonly IServiceProvider serviceProvider;

        public SnykActivityLogger(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }        

        public void LogInformation(string message) => 
            GetActivityLog()?.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
                this.ToString(),
                string.Format(CultureInfo.CurrentCulture,
                LogMessageTemplate, message));

        public void LogError(string message) =>
            GetActivityLog()?.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                this.ToString(),
                string.Format(CultureInfo.CurrentCulture,
                LogMessageTemplate, message));

        private IVsActivityLog GetActivityLog() => serviceProvider.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
    }    
}
