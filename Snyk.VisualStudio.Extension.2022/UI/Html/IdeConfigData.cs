// ABOUTME: Data classes for deserializing IDE configuration JSON from HTML settings
// ABOUTME: Used by HtmlSettingsScriptingBridge to parse configuration with strong typing

using System.Collections.Generic;
using Newtonsoft.Json;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Represents the IDE configuration posted from the LS-served settings HTML (and the
    /// CLI-only fallback HTML). Both forms emit a flat, snake_case pflag-keyed payload — the
    /// same key scheme the IDE uses when it sends settings to the Language Server (see
    /// <see cref="LsSettingsV25"/> / PflagKeys). Every field therefore binds explicitly to its
    /// snake_case JSON key; relying on default camelCase matching silently drops snake_case keys.
    /// <para>
    /// <b>Changed-only / false-vs-absent contract.</b> The form posts only the fields the user
    /// actually changed (see <c>collectChangedData</c> in the LS form JS), so the payload is a
    /// PATCH, not a full snapshot. Each field is therefore nullable and the bridge MUST treat:
    /// <list type="bullet">
    ///   <item><description><b>absent / null</b> = "not changed" → leave the corresponding
    ///   <c>Options</c> value untouched. Applying a default here would silently revert settings
    ///   the user didn't touch (e.g. wiping every other severity filter when one toggles).</description></item>
    ///   <item><description><b>present with a value</b> (including <c>false</c> and empty string)
    ///   = "the user set this" → apply it. <c>false</c> is a real value, NOT the same as absent —
    ///   which is exactly why bool fields are <c>bool?</c> rather than <c>bool</c>.</description></item>
    /// </list>
    /// Every <c>Apply*</c> helper in <see cref="HtmlSettingsScriptingBridge"/> guards on
    /// <c>HasValue</c> / <c>!= null</c> to honour this. A consequence: if a future LS HTML build
    /// renames or adds a key, that field deserialises to null / is dropped. To stop that being
    /// silent, the save path runs <see cref="IdeConfigContract"/> over the raw payload first — it
    /// warns (naming the keys) when some posted keys are unmapped and fails the save outright when
    /// none are recognised (a wholesale rename, which would otherwise no-op yet report success). It
    /// also fails fast when the whole payload deserialises to null (malformed JSON).
    /// </para>
    /// </summary>
    public class IdeConfigData
    {
        // Form type — the CLI-only fallback form posts camelCase "isFallbackForm".
        [JsonProperty("isFallbackForm")]
        public bool? IsFallbackForm { get; set; }

        // Product enablement
        [JsonProperty("snyk_oss_enabled")]
        public bool? SnykOssEnabled { get; set; }

        [JsonProperty("snyk_code_enabled")]
        public bool? SnykCodeEnabled { get; set; }

        [JsonProperty("snyk_iac_enabled")]
        public bool? SnykIacEnabled { get; set; }

        [JsonProperty("snyk_secrets_enabled")]
        public bool? SnykSecretsEnabled { get; set; }

        // Scan
        [JsonProperty("scan_automatic")]
        public bool? ScanAutomatic { get; set; }

        [JsonProperty("scan_net_new")]
        public bool? ScanNetNew { get; set; }

        // Severity filter (flat snake_case keys, not a nested object)
        [JsonProperty("severity_filter_critical")]
        public bool? SeverityFilterCritical { get; set; }

        [JsonProperty("severity_filter_high")]
        public bool? SeverityFilterHigh { get; set; }

        [JsonProperty("severity_filter_medium")]
        public bool? SeverityFilterMedium { get; set; }

        [JsonProperty("severity_filter_low")]
        public bool? SeverityFilterLow { get; set; }

        // Issue view (flat snake_case keys, not a nested object)
        [JsonProperty("issue_view_open_issues")]
        public bool? IssueViewOpenIssues { get; set; }

        [JsonProperty("issue_view_ignored_issues")]
        public bool? IssueViewIgnoredIssues { get; set; }

        // Connection & authentication
        [JsonProperty("api_endpoint")]
        public string ApiEndpoint { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("organization")]
        public string Organization { get; set; }

        [JsonProperty("authentication_method")]
        public string AuthenticationMethod { get; set; }

        [JsonProperty("proxy_insecure")]
        public bool? Insecure { get; set; }

        // CLI settings
        [JsonProperty("cli_path")]
        public string CliPath { get; set; }

        [JsonProperty("automatic_download")]
        public bool? ManageBinariesAutomatically { get; set; }

        [JsonProperty("binary_base_url")]
        public string CliBaseDownloadURL { get; set; }

        [JsonProperty("cli_release_channel")]
        public string CliReleaseChannel { get; set; }

        // Filtering
        [JsonProperty("risk_score_threshold")]
        public int? RiskScoreThreshold { get; set; }

        // Global (Project Defaults) advanced settings — top-level keys, distinct from
        // the per-folder additional_* fields on FolderConfigData.
        [JsonProperty("additional_environment")]
        public string AdditionalEnv { get; set; }

        [JsonProperty("additional_parameters")]
        public List<string> AdditionalParameters { get; set; }

        // Trusted folders
        [JsonProperty("trusted_folders")]
        public List<string> TrustedFolders { get; set; }

        // Folder configs (key stays camelCase: both forms emit "folderConfigs")
        [JsonProperty("folderConfigs")]
        public List<FolderConfigData> FolderConfigs { get; set; }
    }

    /// <summary>
    /// Per-solution/folder configuration. Folder fields are flattened by the form from
    /// <c>folder_INDEX_FIELD</c> input names into snake_case keys.
    /// <para>
    /// The form sends a changed-only folder object (just the fields that differ, plus
    /// <c>folderPath</c>). Every field is therefore nullable so the bridge can tell "absent"
    /// from a real value and avoid clobbering unchanged sibling fields.
    /// </para>
    /// </summary>
    public class FolderConfigData
    {
        [JsonProperty("folderPath")]
        public string FolderPath { get; set; }

        [JsonProperty("additional_parameters")]
        public List<string> AdditionalParameters { get; set; }

        [JsonProperty("additional_environment")]
        public string AdditionalEnv { get; set; }

        [JsonProperty("preferred_org")]
        public string PreferredOrg { get; set; }

        [JsonProperty("auto_determined_org")]
        public string AutoDeterminedOrg { get; set; }

        [JsonProperty("org_set_by_user")]
        public bool? OrgSetByUser { get; set; }

        [JsonProperty("base_branch")]
        public string BaseBranch { get; set; }

        [JsonProperty("scan_command_config")]
        public Dictionary<string, ScanCommandConfig> ScanCommandConfig { get; set; }

        // Per-folder org-scope overrides rendered in the form's per-folder section. Nullable so an
        // absent field (changed-only payload) means "no change", not "clear the override".
        [JsonProperty("snyk_oss_enabled")]
        public bool? SnykOssEnabled { get; set; }

        [JsonProperty("snyk_code_enabled")]
        public bool? SnykCodeEnabled { get; set; }

        [JsonProperty("snyk_iac_enabled")]
        public bool? SnykIacEnabled { get; set; }

        [JsonProperty("snyk_secrets_enabled")]
        public bool? SnykSecretsEnabled { get; set; }

        [JsonProperty("scan_automatic")]
        public bool? ScanAutomatic { get; set; }

        [JsonProperty("scan_net_new")]
        public bool? ScanNetNew { get; set; }

        [JsonProperty("severity_filter_critical")]
        public bool? SeverityFilterCritical { get; set; }

        [JsonProperty("severity_filter_high")]
        public bool? SeverityFilterHigh { get; set; }

        [JsonProperty("severity_filter_medium")]
        public bool? SeverityFilterMedium { get; set; }

        [JsonProperty("severity_filter_low")]
        public bool? SeverityFilterLow { get; set; }

        [JsonProperty("issue_view_open_issues")]
        public bool? IssueViewOpenIssues { get; set; }

        [JsonProperty("issue_view_ignored_issues")]
        public bool? IssueViewIgnoredIssues { get; set; }

        [JsonProperty("risk_score_threshold")]
        public int? RiskScoreThreshold { get; set; }
    }
}
