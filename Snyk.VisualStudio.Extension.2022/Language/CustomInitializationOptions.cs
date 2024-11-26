using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snyk.VisualStudio.Extension.Language
{
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
        public string ActivateSnykCodeQuality { get; set; }
        public string DeviceId { get; set; }
        public string IntegrationName { get; set; }
        public string IntegrationVersion { get; set; }
        public FilterSeverityOptions FilterSeverity { get; set; }
        public string ScanningMode { get; set; }
        public string AuthenticationMethod { get; set; }
        public string SnykCodeApi { get; set; }
        public int HoverVerbosity { get; set; }
        public string OutputFormat { get; set; }
        public string EnableDeltaFindings { get; set; }
        public List<FolderConfig> FolderConfigs { get; set; }
    }

    public class FolderConfig
    {
        public string BaseBranch { get; set; }
        public string FolderPath { get; set; }
        public List<string> LocalBranches { get; set; }
        public List<string> AdditionalParameters { get; set; }
    }

    public class FilterSeverityOptions
    {
        public bool Critical { get; set; }
        public bool High { get; set; }
        public bool Medium { get; set; }
        public bool Low { get; set; }
    }
}
