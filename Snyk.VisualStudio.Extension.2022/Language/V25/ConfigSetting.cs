using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snyk.VisualStudio.Extension.Language
{
    // Unified wire type for the map-based configuration protocol (v25+).
    // Mirrors snyk-ls internal/types/lsp.go ConfigSetting.
    // IDE→LS: set Changed=true and Value; omit entries you don't want to touch.
    // LS→IDE: all fields populated (Value, Source, OriginScope, IsLocked).
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ConfigSetting
    {
        public object Value { get; set; }
        public bool Changed { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string OriginScope { get; set; }

        // Omit when false (IDE→LS messages never lock; only LS→IDE populates this).
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsLocked { get; set; }

        public static ConfigSetting Of(object value) => new ConfigSetting { Value = value, Changed = true };
    }
}
