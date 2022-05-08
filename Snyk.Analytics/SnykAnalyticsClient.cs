using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Snyk.Analytics
{
    public class SnykAnalyticsClient : ISnykAnalyticsService
    {
        public static async Task<SnykAnalyticsClient> CreateAsync()
        {
            await Task.Delay(1);
            return null;
        }

        public void LogAnalysisReadyEvent(string analysisType, string analysisResult)
        {
            throw new NotImplementedException();
        }

        public void LogWelcomeIsViewedEvent()
        {
            throw new NotImplementedException();
        }

        public void LogAnalysisIsTriggeredEvent(IList<string> selectedProducts)
        {
            throw new NotImplementedException();
        }

        public void LogIssueIsViewedEvent(string id, string issueType, string severity)
        {
            throw new NotImplementedException();
        }

        public void ObtainUser(string apiToken)
        {
            throw new NotImplementedException();
        }

        public bool AnalyticsEnabled { get; set; }
        
        public string UserIdAsHash { get; } = "";

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
