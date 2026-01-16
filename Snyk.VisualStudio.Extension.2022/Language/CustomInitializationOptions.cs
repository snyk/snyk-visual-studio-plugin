// ABOUTME: This file defines initialization options and configuration structures for the Snyk Language Server protocol
// ABOUTME: It contains data models for folder configs, scan commands, and initialization parameters sent to the Language Server
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snyk.VisualStudio.Extension.Language
{
    /// <summary>
    /// CamelCase naming strategy that preserves dictionary keys as-is.
    /// </summary>
    public class CamelCasePreserveDictionaryKeysNamingStrategy : CamelCaseNamingStrategy
    {
        public CamelCasePreserveDictionaryKeysNamingStrategy() : base(processDictionaryKeys: false, overrideSpecifiedNames: false)
        {
        }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SnykLsInitializationOptions
    {
        public string RequiredProtocolVersion { get; set; }
        public string ActivateSnykOpenSource { get; set; }
        public string ActivateSnykCode { get; set; }
        public string ActivateSnykIac { get; set; }
        public string Insecure { get; set; }
        public string Endpoint { get; set; }
        public string AdditionalParams { get; set; }
        public string AdditionalEnv { get; set; }
        public string Path { get; set; }
        public string SendErrorReports { get; set; }
        public string Organization { get; set; }
        public string EnableTelemetry { get; set; }
        public string ManageBinariesAutomatically { get; set; }
        public string CliPath { get; set; }
        public string Token { get; set; }
        public string AutomaticAuthentication { get; set; }
        public string EnableTrustedFoldersFeature { get; set; }
        public List<string> TrustedFolders { get; set; }
        public string ActivateSnykCodeSecurity { get; set; }
        public string DeviceId { get; set; }
        public string IntegrationName { get; set; }
        public string IntegrationVersion { get; set; }
        public FilterSeverityOptions FilterSeverity { get; set; }
        public IssueViewOptions IssueViewOptions { get; set; }
        public string ScanningMode { get; set; }
        public string AuthenticationMethod { get; set; }
        public string SnykCodeApi { get; set; }
        public int HoverVerbosity { get; set; }
        public string OutputFormat { get; set; }
        public string EnableDeltaFindings { get; set; }
        public List<FolderConfig> FolderConfigs { get; set; }
        public string CliBaseDownloadUrl { get; set; }
        public int? RiskScoreThreshold { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCasePreserveDictionaryKeysNamingStrategy))]
    public class FolderConfig
    {
        public string FolderPath { get; set; }
        public string BaseBranch { get; set; }
        public List<string> LocalBranches { get; set; }
        public List<string> AdditionalParameters { get; set; }
        public string AdditionalEnv { get; set; }
        public string ReferenceFolderPath { get; set; }
        public Dictionary<string, ScanCommandConfig> ScanCommandConfig { get; set; }
        public string PreferredOrg { get; set; }
        public string AutoDeterminedOrg { get; set; }
        public bool OrgMigratedFromGlobalConfig { get; set; }
        public bool OrgSetByUser { get; set; }

        public void SetScanCommandConfig(Dictionary<string, ScanCommandConfig> scanCommandConfig)
        {
            this.ScanCommandConfig = scanCommandConfig;
        }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ScanCommandConfig
    {
        public string PreScanCommand { get; set; }
        public bool PreScanOnlyReferenceFolder { get; set; }
        public string PostScanCommand { get; set; }
        public bool PostScanOnlyReferenceFolder { get; set; }
    }

    public class FolderConfigsParam
    {
        public List<FolderConfig> FolderConfigs { get; set; }
    }
    public class ScanSummaryParam
    {
        public string ScanSummary { get; set; }
    }

    public class FilterSeverityOptions
    {
        public bool Critical { get; set; }
        public bool High { get; set; }
        public bool Medium { get; set; }
        public bool Low { get; set; }
    }

    public class IssueViewOptions
    {
        public bool OpenIssues { get; set; }
        public bool IgnoredIssues { get; set; }
    }
}
