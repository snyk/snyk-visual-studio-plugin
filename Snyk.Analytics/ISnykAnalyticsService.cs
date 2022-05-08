using System.Collections.Generic;

namespace Snyk.Analytics
{
    public interface ISnykAnalyticsService : IDisposable
    {
        /// <summary>
        /// Log Analysis Is Ready event.
        /// </summary>
        /// <param name="analysisType">Type of analysis (Oss, SnykCode Security or quality).</param>
        /// <param name="analysisResult">Analysis result (success or error).</param>
        void LogAnalysisReadyEvent(string analysisType, string analysisResult);

        /// <summary>
        /// Log UserLandedOnTheWelcomePageEvent.
        /// </summary>
        void LogWelcomeIsViewedEvent();

        /// <summary>
        /// Log UserTriggersAnAnalysisEvent.
        /// </summary>
        /// <param name="selectedProducts">Selected products (OSS, SnykCode Quality and Security).</param>
        void LogAnalysisIsTriggeredEvent(IList<string> selectedProducts);

        /// <summary>
        /// Log Issue Is Viewed Event.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <param name="issueType">Type of issue (Oss, SnykCode Security or Queality.</param>
        /// <param name="severity">Severity name.</param>
        void LogIssueIsViewedEvent(string id, string issueType, string severity);
    }
}