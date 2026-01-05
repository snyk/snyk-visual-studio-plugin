// ABOUTME: Data classes for deserializing IDE configuration JSON from HTML settings
// ABOUTME: Used by ConfigScriptingBridge to parse configuration with strong typing

using System.Collections.Generic;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Represents the complete IDE configuration sent from LS HTML JavaScript.
    /// </summary>
    public class IdeConfigData
    {
        // Scan Settings
        public bool? ActivateSnykOpenSource { get; set; }
        public bool? ActivateSnykCode { get; set; }
        public bool? ActivateSnykIac { get; set; }
        public string ScanningMode { get; set; }

        // Issue View Settings
        public IssueViewOptions IssueViewOptions { get; set; }
        public bool? EnableDeltaFindings { get; set; }

        // Authentication Settings
        public string AuthenticationMethod { get; set; }

        // Connection Settings
        public string Endpoint { get; set; }
        public string Token { get; set; }
        public string Organization { get; set; }
        public bool? Insecure { get; set; }

        // Trusted Folders
        public List<string> TrustedFolders { get; set; }

        // CLI Settings
        public string CliPath { get; set; }
        public bool? ManageBinariesAutomatically { get; set; }
        public string BaseUrl { get; set; }
        public string CliReleaseChannel { get; set; }

        // Filter Settings
        public FilterSeverity FilterSeverity { get; set; }

        // Miscellaneous Settings
        public int? RiskScoreThreshold { get; set; }

        // Folder Configs
        public List<FolderConfigData> FolderConfigs { get; set; }
    }

    /// <summary>
    /// Issue view configuration options.
    /// </summary>
    public class IssueViewOptions
    {
        public bool OpenIssues { get; set; }
        public bool IgnoredIssues { get; set; }
    }

    /// <summary>
    /// Severity filter configuration.
    /// </summary>
    public class FilterSeverity
    {
        public bool Critical { get; set; }
        public bool High { get; set; }
        public bool Medium { get; set; }
        public bool Low { get; set; }
    }

    /// <summary>
    /// Per-solution/folder configuration.
    /// </summary>
    public class FolderConfigData
    {
        public string AdditionalParameters { get; set; }
        public string AdditionalEnv { get; set; }
        public string PreferredOrg { get; set; }
        public string AutoDeterminedOrg { get; set; }
        public bool OrgSetByUser { get; set; }
        public Dictionary<string, ScanCommandConfig> ScanCommandConfig { get; set; }
    }
}
