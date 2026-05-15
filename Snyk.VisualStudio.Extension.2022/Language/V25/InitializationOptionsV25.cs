using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snyk.VisualStudio.Extension.Language
{
    // Sent once during LSP initialize (protocol v25+).
    // Settings use pflag canonical names as keys. Metadata fields are init-only.
    // Mirrors snyk-ls internal/types/lsp.go InitializationOptions.
    [JsonObject(NamingStrategyType = typeof(CamelCasePreserveDictionaryKeysNamingStrategy))]
    public class InitializationOptionsV25
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ConfigSetting> Settings { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<LspFolderConfig> FolderConfigs { get; set; }

        public string RequiredProtocolVersion { get; set; }
        public string DeviceId { get; set; }
        public string IntegrationName { get; set; }
        public string IntegrationVersion { get; set; }
        public string OsPlatform { get; set; }
        public string OsArch { get; set; }
        public string RuntimeName { get; set; }
        public string RuntimeVersion { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? HoverVerbosity { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string OutputFormat { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }
    }
}
