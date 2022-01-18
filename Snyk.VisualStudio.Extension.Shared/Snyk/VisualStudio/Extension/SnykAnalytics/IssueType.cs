namespace Snyk.VisualStudio.Extension.Shared.SnykAnalytics
{
    using System;
    using Snyk.Code.Library.Domain.Analysis;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    
    /// <summary>
    /// Issue type for analytics event.
    /// </summary>
    public class IssueType
    {
        /// <summary>
        /// Open Source Vulnerability type.
        /// </summary>
        public const string OpenSourceVulnerability = "Open Source Vulnerability";

        /// <summary>
        /// Licence Issue type.
        /// </summary>
        public const string LicenceIssue = "Licence Issue";

        /// <summary>
        /// Code Quality Issue type.
        /// </summary>
        public const string CodeQualityIssue = "Code Quality Issue";

        /// <summary>
        /// Code Security Vulnerability type.
        /// </summary>
        public const string CodeSecurityVulnerability = "Code Security Vulnerability";

        internal static string Get(Vulnerability vulnerability) => vulnerability.IsLicense() ? LicenceIssue : OpenSourceVulnerability;

        internal static string Get(Suggestion suggestion) => suggestion.Categories.Contains("Security") ? CodeSecurityVulnerability : CodeQualityIssue;
    }
}
