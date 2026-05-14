// ABOUTME: Data classes for deserializing IDE configuration JSON from HTML settings
// ABOUTME: Used by ConfigScriptingBridge to parse configuration with strong typing

using System.Collections.Generic;
using Newtonsoft.Json;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Represents the complete IDE configuration sent from LS HTML JavaScript.
    /// </summary>
    public class IdeConfigData
    {
        // Form Type
        public bool? IsFallbackForm { get; set; }

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

        // The fallback HTML form posts this field as "proxy_insecure" (snake_case); the
        // full LS-served form's contract on the same flag also lands here. Explicit
        // JsonProperty so the property does not silently miss the JSON key.
        [JsonProperty("proxy_insecure")]
        public bool? Insecure { get; set; }

        // Trusted Folders
        public List<string> TrustedFolders { get; set; }

        // CLI Settings — the fallback HTML form uses snake_case JSON keys that do not match
        // these PascalCase C# property names by default. Explicit JsonProperty bindings keep
        // the form's saves landing on the right fields.
        [JsonProperty("cli_path")]
        public string CliPath { get; set; }

        [JsonProperty("automatic_download")]
        public bool? ManageBinariesAutomatically { get; set; }

        [JsonProperty("binary_base_url")]
        public string CliBaseDownloadURL { get; set; }

        [JsonProperty("cli_release_channel")]
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
        public List<string> AdditionalParameters { get; set; }
        public string AdditionalEnv { get; set; }
        public string PreferredOrg { get; set; }
        public string AutoDeterminedOrg { get; set; }
        public bool OrgSetByUser { get; set; }
        public Dictionary<string, ScanCommandConfig> ScanCommandConfig { get; set; }
    }
}
