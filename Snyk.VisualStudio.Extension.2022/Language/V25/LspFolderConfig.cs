using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snyk.VisualStudio.Extension.Language
{
    // Per-folder settings embedded in LspConfigurationParam.
    // Mirrors snyk-ls internal/types/lsp.go LspFolderConfig.
    [JsonObject(NamingStrategyType = typeof(CamelCasePreserveDictionaryKeysNamingStrategy))]
    public class LspFolderConfig
    {
        public string FolderPath { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ConfigSetting> Settings { get; set; }
    }
}
