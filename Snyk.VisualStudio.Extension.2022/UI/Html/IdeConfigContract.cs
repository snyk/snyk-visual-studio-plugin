// ABOUTME: Detects drift between the LS-served settings HTML payload and this plugin's IdeConfigData bindings.
// ABOUTME: Newtonsoft silently drops posted keys with no [JsonProperty], so a newer form could add a setting the IDE never saves.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Guards the settings-save contract. The settings HTML is synced from snyk-ls and can gain
    /// fields independently of this plugin; <see cref="JsonConvert"/> silently ignores any posted key
    /// that has no matching <see cref="JsonPropertyAttribute"/> on <see cref="IdeConfigData"/>, so a
    /// newer form could carry a setting the IDE never persists with no error. This reports which
    /// posted top-level keys are unmapped so the save path can warn (and fail loudly when the whole
    /// payload is unrecognised — a wholesale rename that would otherwise no-op and report success).
    /// </summary>
    public static class IdeConfigContract
    {
        // The snake_case keys IdeConfigData binds, derived from its [JsonProperty] attributes so the
        // set stays correct automatically as fields are added/removed — no second list to maintain.
        private static readonly HashSet<string> BoundKeys = BuildBoundKeys(typeof(IdeConfigData));

        /// <summary>
        /// Inspects a settings payload and reports the top-level keys that <see cref="IdeConfigData"/>
        /// does not bind. Per-folder keys (inside <c>folderConfigs[]</c>) are NOT checked: the IDE
        /// forwards every folder key verbatim to the LS without modeling them, so "unmapped" has no
        /// meaning there. A non-object/unparseable payload yields an empty result (handled elsewhere).
        /// </summary>
        public static UnmappedKeysResult Analyze(string json)
        {
            JObject root;
            try
            {
                root = JToken.Parse(json) as JObject;
            }
            catch
            {
                return new UnmappedKeysResult(0, new List<string>());
            }

            if (root == null)
                return new UnmappedKeysResult(0, new List<string>());

            var incoming = root.Properties().Select(p => p.Name).ToList();
            var unmapped = incoming.Where(k => !BoundKeys.Contains(k)).ToList();

            return new UnmappedKeysResult(incoming.Count, unmapped);
        }

        private static HashSet<string> BuildBoundKeys(Type type)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var prop in type.GetProperties())
            {
                var attr = prop.GetCustomAttribute<JsonPropertyAttribute>();
                keys.Add(attr?.PropertyName ?? prop.Name);
            }

            return keys;
        }
    }

    /// <summary>Result of <see cref="IdeConfigContract.Analyze"/>.</summary>
    public sealed class UnmappedKeysResult
    {
        public UnmappedKeysResult(int totalKeys, IReadOnlyList<string> unmappedKeys)
        {
            this.TotalKeys = totalKeys;
            this.UnmappedKeys = unmappedKeys;
        }

        /// <summary>Number of top-level keys in the payload.</summary>
        public int TotalKeys { get; }

        /// <summary>Top-level keys with no matching IdeConfigData binding.</summary>
        public IReadOnlyList<string> UnmappedKeys { get; }

        /// <summary>True when the payload had keys but none of them were recognised (wholesale rename).</summary>
        public bool AllUnmapped => this.TotalKeys > 0 && this.UnmappedKeys.Count == this.TotalKeys;

        /// <summary>True when any top-level key was unmapped.</summary>
        public bool HasUnmappedKeys => this.UnmappedKeys.Count > 0;
    }
}
