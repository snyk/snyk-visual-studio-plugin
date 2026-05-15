using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snyk.VisualStudio.Extension.Language
{
    // Payload for $/snyk.configuration notification and DidChangeConfiguration (v25+).
    // Mirrors snyk-ls internal/types/lsp.go LspConfigurationParam.
    [JsonObject(NamingStrategyType = typeof(CamelCasePreserveDictionaryKeysNamingStrategy))]
    public class LspConfigurationParam
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ConfigSetting> Settings { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<LspFolderConfig> FolderConfigs { get; set; }
    }
}
