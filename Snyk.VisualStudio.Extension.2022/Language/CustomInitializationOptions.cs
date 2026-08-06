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
    /// Payload for <c>workspace/didChangeWorkspaceFolders</c>.
    ///
    /// Visual Studio decides the workspace folders it passes at initialize time, so a server activated
    /// before the solution has loaded gets none and would otherwise run against an empty workspace for
    /// its whole life — the folder set is not re-read. This notification is how a folder is handed over
    /// afterwards; snyk-ls builds a real folder from it, configures it, and scans it.
    ///
    /// Property names are explicit because the casing is asymmetric on the server side: snyk-ls tags the
    /// envelope fields <c>Event</c>/<c>Added</c>/<c>Removed</c> but the folder fields <c>uri</c>/
    /// <c>name</c> (internal/types/lsp.go). Relying on a naming strategy here would silently send fields
    /// the server ignores.
    /// </summary>
    public class DidChangeWorkspaceFoldersParams
    {
        [JsonProperty("Event")]
        public WorkspaceFoldersChangeEvent Event { get; set; }
    }

    /// <summary>The added and removed folders for <see cref="DidChangeWorkspaceFoldersParams"/>.</summary>
    public class WorkspaceFoldersChangeEvent
    {
        [JsonProperty("Added", NullValueHandling = NullValueHandling.Ignore)]
        public List<LspWorkspaceFolder> Added { get; set; }

        [JsonProperty("Removed", NullValueHandling = NullValueHandling.Ignore)]
        public List<LspWorkspaceFolder> Removed { get; set; }
    }

    /// <summary>A single workspace folder, as snyk-ls expects it on the wire.</summary>
    public class LspWorkspaceFolder
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
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
        }

        // Stores the value (incl. null for a reset → {value:null, changed:true} on the wire so the
        // LS Unsets the user:folder: override). A re-set in the same cycle simply overwrites.
        public void Set(string key, object value) => Settings[key] = ConfigSetting.Of(value);

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
