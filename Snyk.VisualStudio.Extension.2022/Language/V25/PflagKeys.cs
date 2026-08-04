using System.Collections.Generic;

namespace Snyk.VisualStudio.Extension.Language
{
    // Canonical pflag setting key constants — mirrors snyk-ls internal/types/ldx_sync_config.go.
    internal static class PflagKeys
    {
        // Products
        public const string SnykOssEnabled = "snyk_oss_enabled";
        public const string SnykCodeEnabled = "snyk_code_enabled";
        public const string SnykIacEnabled = "snyk_iac_enabled";
        public const string SnykSecretsEnabled = "snyk_secrets_enabled";

        // Scan
        public const string ScanAutomatic = "scan_automatic";
        public const string ScanNetNew = "scan_net_new";

        // Severity filters (one key per level)
        public const string SeverityFilterCritical = "severity_filter_critical";
        public const string SeverityFilterHigh = "severity_filter_high";
        public const string SeverityFilterMedium = "severity_filter_medium";
        public const string SeverityFilterLow = "severity_filter_low";

        // Issue view
        public const string IssueViewOpenIssues = "issue_view_open_issues";
        public const string IssueViewIgnoredIssues = "issue_view_ignored_issues";

        // Risk score
        public const string RiskScoreThreshold = "risk_score_threshold";

        // Connection / auth
        public const string ApiEndpoint = "api_endpoint";
        public const string Token = "token";
        public const string Organization = "organization";
        public const string AuthenticationMethod = "authentication_method";
        public const string ProxyInsecure = "proxy_insecure";

        // CLI / binary
        public const string AutomaticDownload = "automatic_download";
        public const string CliPath = "cli_path";
        public const string BinaryBaseUrl = "binary_base_url";
        public const string CliReleaseChannel = "cli_release_channel";

        // Trust
        public const string TrustedFolders = "trusted_folders";
        // trust_enabled is defined in the LS spec and belongs to the always-changed set, but is not
        // yet emitted by BuildSettingsMap — trust enforcement still lives on the IDE side.
        public const string TrustEnabled = "trust_enabled";
        // Emitted as an unconditional false by BuildSettingsMap via ConfigSetting.Of, so it is
        // deliberately NOT in the always-changed set below — that set is for tracker-gated keys.
        public const string AutomaticAuthentication = "automatic_authentication";

        // Folder-level
        public const string AdditionalParameters = "additional_parameters";
        public const string AdditionalEnvironment = "additional_environment";
        public const string PreferredOrg = "preferred_org";
        public const string OrgSetByUser = "org_set_by_user";
        public const string AutoDeterminedOrg = "auto_determined_org";
        public const string ScanCommandConfig = "scan_command_config";
        public const string BaseBranch = "base_branch";
        public const string LocalBranches = "local_branches";
        public const string ReferenceFolder = "reference_folder";

        // Also sent as top-level fields in InitializationOptionsV25; snyk-ls reads them from
        // there, not from the Settings map — the Settings-map copies are harmless redundancy.
        public const string ClientProtocolVersion = "client_protocol_version";
        public const string DeviceId = "device_id";

        // Keys that are always sent with changed:true regardless of user action (requirement M4).
        // trusted_folders must always signal intent so the LS never silently inherits an org default.
        // Private to prevent external mutation; callers use IsAlwaysChanged(key).
        private static readonly HashSet<string> _alwaysChanged = new HashSet<string>
        {
            TrustedFolders,
            TrustEnabled,
            // cli_path: the IDE downloads and owns the CLI binary, so the LS must run the one we
            // installed. Sent with changed:false the LS discards it (settingStr ignores unchanged
            // entries) and resolves its registered default of $XDG_DATA_HOME/snyk-ls instead — a
            // different binary from the one we manage, for its own CLI invocations.
            // The inbound direction is handled separately: GlobalSettingsApplier ignores cli_path,
            // so that default can never come back and repoint us at an empty location. The other
            // plugins do not guard that direction yet.
            // Matches VS Code on this outbound resolution, which materialises the resolved path.
            CliPath,
        };

        /// <summary>
        /// Returns true when <paramref name="key"/> must always be sent with <c>changed:true</c>
        /// regardless of whether the user has explicitly overridden it (requirement M4).
        /// </summary>
        public static bool IsAlwaysChanged(string key) => _alwaysChanged.Contains(key);

        // Global (Project Defaults / org-scope) settings the user can reset to default via the
        // "Reset overrides" button. Mirrors snyk-ls GlobalResettableSettings (IDE-2149): a form save
        // that posts one of these keys as explicit JSON null is a reset — the plugin un-marks the
        // local override and sends {value:null, changed:true} so the LS Unsets the user:global
        // override and the org/LDX-sync default takes effect. Private; callers use IsGlobalResettable.
        private static readonly HashSet<string> _globalResettable = new HashSet<string>
        {
            SnykOssEnabled,
            SnykCodeEnabled,
            SnykIacEnabled,
            SnykSecretsEnabled,
            ScanAutomatic,
            ScanNetNew,
            SeverityFilterCritical,
            SeverityFilterHigh,
            SeverityFilterMedium,
            SeverityFilterLow,
            IssueViewOpenIssues,
            IssueViewIgnoredIssues,
            RiskScoreThreshold,
            Organization,
        };

        /// <summary>
        /// Returns true when <paramref name="key"/> is a global (Project Defaults) setting that the
        /// user can reset to default. A form save posting this key as explicit JSON null is a reset
        /// (IDE-2152). Mirrors snyk-ls <c>GlobalResettableSettings</c>.
        /// </summary>
        public static bool IsGlobalResettable(string key) => _globalResettable.Contains(key);
    }
}
