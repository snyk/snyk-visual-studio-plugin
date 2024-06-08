using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Snyk.VisualStudio.Extension.Shared.Language
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SnykLSInitializationOptions
    {
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
    }

    public class FilterSeverityOptions
    {
        public bool Critical { get; set; }
        public bool High { get; set; }
        public bool Medium { get; set; }
        public bool Low { get; set; }
    }
}
