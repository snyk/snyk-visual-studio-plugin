// ABOUTME: This file defines initialization options and configuration structures for the Snyk Language Server protocol
// ABOUTME: It contains data models for folder configs, scan commands, and initialization parameters sent to the Language Server
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    /// <summary>
    /// Per-folder configuration. snyk-ls is authoritative over folder-scoped settings, so this is an
    /// opaque pflag-keyed settings map round-tripped verbatim (matching vscode/eclipse) rather than a
    /// set of typed fields the IDE cherry-picks. The IDE is "dumb": any folder key the LS sends is
    /// stored and echoed back without the IDE needing to model it. The few keys the IDE itself reads
    /// or writes (base branch, local branches, reference folder, additional params for debug level)
    /// go through the typed accessors below, keyed by <see cref="PflagKeys"/>.
    /// <para>
    /// <see cref="Settings"/> serializes verbatim to disk and over the wire as the LspFolderConfig
    /// <c>settings</c> map. On-disk entries written by older builds carried typed props instead; they
    /// are tolerated (unknown JSON props are ignored on load) and the LS repopulates the map on its
    /// next <c>$/snyk.configuration</c> push, so the user-set keys it persists (preferred_org,
    /// org_set_by_user, reference_folder, base_branch) come back.
    /// </para>
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCasePreserveDictionaryKeysNamingStrategy))]
    public class FolderConfig
    {
        public string FolderPath { get; set; }

        // The pflag-keyed folder settings map, verbatim. Each value is a ConfigSetting wrapping the
        // raw value (the LS may also populate Source/OriginScope/IsLocked metadata). Round-tripped
        // unchanged: inbound from the LS, persisted, and sent back on DidChangeConfiguration.
        public Dictionary<string, ConfigSetting> Settings { get; set; } = new Dictionary<string, ConfigSetting>();

        // LS keys the user reset via the settings dialog (sent as JSON null). A present-null can't be
        // distinguished from absent once mapped, so resets are tracked here instead. BuildFolderConfigs
        // emits {value:null, changed:true} for each, which makes snyk-ls Unset the user:folder:
        // override (fallback to org/LDX/default). Transient + one-shot: not persisted to disk,
        // consumed within the same in-memory save→DidChangeConfiguration cycle.
        [JsonIgnore]
        public HashSet<string> ResetKeys { get; set; }

        // ----- Typed accessors over the opaque map (keyed by PflagKeys.*) -----
        // These let the dialog and the handful of IDE-side readers stay readable without
        // re-introducing typed fields. Values are stored as ConfigSetting.Of(...) so the round-trip
        // back to the LS carries Changed=true.

        public string GetString(string key) => GetValueToken(key)?.Value<string>();

        public List<string> GetStringList(string key) => GetValueToken(key)?.ToObject<List<string>>();

        public void SetString(string key, string value)
        {
            if (value == null) Settings.Remove(key);
            else Settings[key] = ConfigSetting.Of(value);
            ResetKeys?.Remove(key);
        }

        public void Set(string key, object value)
        {
            Settings[key] = ConfigSetting.Of(value);
            // A re-set wins over a pending reset: clearing the ResetKey stops BuildFolderConfigs
            // from clobbering this just-set value back to {value:null} on the next send.
            ResetKeys?.Remove(key);
        }

        // Returns the raw stored value as a JToken for typed extraction. Values arrive either as
        // JTokens (Json.NET deserialization of the LS payload) or as boxed CLR objects (set IDE-side);
        // normalize both to JToken. Null/missing → null.
        private JToken GetValueToken(string key)
        {
            if (Settings == null || !Settings.TryGetValue(key, out var setting) || setting?.Value == null)
                return null;
            return setting.Value is JToken jt ? jt : JToken.FromObject(setting.Value);
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

    public class ScanSummaryParam
    {
        public string ScanSummary { get; set; }
    }

    /// <summary>
    /// Payload of the <c>$/snyk.treeView</c> notification: the server-rendered HTML issue tree.
    /// </summary>
    public class TreeViewParams
    {
        [JsonProperty("treeViewHtml")]
        public string TreeViewHtml { get; set; }

        [JsonProperty("totalIssues")]
        public int TotalIssues { get; set; }
    }

}
