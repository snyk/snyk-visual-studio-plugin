// ABOUTME: Data classes for deserializing IDE configuration JSON from HTML settings
// ABOUTME: Used by HtmlSettingsScriptingBridge to parse configuration with strong typing

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        // Global (Project Defaults) advanced settings — top-level keys, distinct from the
        // per-folder additional_* keys (those are forwarded verbatim, not modeled here).
        [JsonProperty("additional_environment")]
        public string AdditionalEnv { get; set; }

        // Form sends additional_parameters as a raw string (text input); split on whitespace when applying.
        [JsonProperty("additional_parameters")]
        public string AdditionalParameters { get; set; }

        // Trusted folders
        [JsonProperty("trusted_folders")]
        public List<string> TrustedFolders { get; set; }

        // Per-folder settings (folderConfigs: [...]). The inner folder fields are intentionally NOT
        // modeled — ApplyFolderConfigsAsync forwards them verbatim from the raw JSON to the LS (the
        // IDE is "dumb" about folder-scoped keys, matching vscode/eclipse). This property exists only
        // so IdeConfigContract recognizes the top-level "folderConfigs" key as bound; it is otherwise
        // unused (the typed global save path doesn't read it).
        [JsonProperty("folderConfigs")]
        public JArray FolderConfigs { get; set; }
    }
}
