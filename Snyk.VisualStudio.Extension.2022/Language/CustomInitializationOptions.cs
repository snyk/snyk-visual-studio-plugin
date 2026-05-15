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

}
