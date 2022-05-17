namespace Snyk.Analytics
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;


    public interface ISnykAnalyticsService : IDisposable
    {
        /// <summary>
        /// Log Analysis Is Ready event.
        /// </summary>
        /// <param name="analysisType">Type of analysis (Oss, SnykCode Security or quality).</param>
        /// <param name="analysisResult">Analysis result (success or error).</param>
        void LogAnalysisReadyEvent(AnalysisType analysisType, AnalyticsAnalysisResult analysisResult);

        /// <summary>
        /// Log UserLandedOnTheWelcomePageEvent.
        /// </summary>
        void LogWelcomeIsViewedEvent();

        /// <summary>
        /// Log UserTriggersAnAnalysisEvent.
        /// </summary>
        /// <param name="selectedProducts">Selected products (OSS, SnykCode Quality and Security).</param>
        void LogAnalysisIsTriggeredEvent(IList<AnalysisType> selectedProducts);

        /// <summary>
        /// Log Issue Is Viewed Event.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <param name="issueType">Type of issue (Oss, SnykCode Security or Quality.</param>
        /// <param name="severity">Severity of issue (Low/Medium/High/Critical)</param>
        void LogIssueIsViewedEvent(string id, ScanResultIssueType issueType, string severity);

        /// <summary>
        /// Fetch user information for event tracking
        /// </summary>
        /// <param name="apiToken">The API token that's used in authentication</param>
        Task ObtainUserAsync(string apiToken);

        /// <summary>
        /// Fetch user information for event tracking
        /// </summary>
        /// <param name="apiToken">The API token that's used in authentication</param>
        /// <param name="vsVersion">The Visual Studio version</param>
        Task ObtainUserAsync(string apiToken, string vsVersion);

        /// <summary>
        /// Gets or sets a value indicating whether is analytics enabled.
        /// </summary>
        bool AnalyticsEnabledOption { get; set; }

        /// <summary>
        /// Gets a value indicating whether user id hash string.
        /// </summary>
        string UserIdAsHash { get; }
    }
}