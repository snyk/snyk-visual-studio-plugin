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

        // Folder-level
        public const string AdditionalParameters = "additional_parameters";
        public const string AdditionalEnvironment = "additional_environment";
        public const string PreferredOrg = "preferred_org";
        public const string OrgSetByUser = "org_set_by_user";
        public const string AutoDeterminedOrg = "auto_determined_org";
        public const string ScanCommandConfig = "scan_command_config";
        public const string BaseBranch = "base_branch";

        // Also sent as top-level fields in InitializationOptionsV25; snyk-ls reads them from
        // there, not from the Settings map — the Settings-map copies are harmless redundancy.
        public const string ClientProtocolVersion = "client_protocol_version";
        public const string DeviceId = "device_id";
    }
}
