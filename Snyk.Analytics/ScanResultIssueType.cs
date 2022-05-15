namespace Snyk.Analytics
{
    using System;
    using Iteratively;

    public enum ScanResultIssueType
    {
        OpenSourceVulnerability,
        LicenceIssue,
        CodeQualityIssue,
        CodeSecurityVulnerability,
    }

    public static class ScanResultIssueTypeExtensions
    {
        public static IssueInTreeIsClicked.IssueType ToIssueInTreeIsClickedEnum(this ScanResultIssueType issueTypeParam)
        {
            switch (issueTypeParam)
            {
                case ScanResultIssueType.OpenSourceVulnerability:
                    return IssueInTreeIsClicked.IssueType.OpenSourceVulnerability;
                case ScanResultIssueType.LicenceIssue:
                    return IssueInTreeIsClicked.IssueType.LicenceIssue;
                case ScanResultIssueType.CodeQualityIssue:
                    return IssueInTreeIsClicked.IssueType.CodeQualityIssue;
                case ScanResultIssueType.CodeSecurityVulnerability:
                    return IssueInTreeIsClicked.IssueType.CodeSecurityVulnerability;
                default:
                    throw new ArgumentOutOfRangeException(nameof(issueTypeParam));
            }

        }
    }
}