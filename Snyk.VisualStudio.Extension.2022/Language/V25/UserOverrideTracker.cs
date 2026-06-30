// ABOUTME: Tracks which global LSP pflag keys the user explicitly overrode from plugin defaults.
// ABOUTME: Persists via SnykSettings.ChangedConfigKeys; seeds from options on first load (IDE-2152).
using System;
using System.Collections.Generic;
using System.Linq;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Language
{
    /// <inheritdoc cref="IUserOverrideTracker"/>
    public class UserOverrideTracker : IUserOverrideTracker
    {
        // Keys that the user has explicitly set away from plugin defaults.
        private readonly HashSet<string> changed = new HashSet<string>();

        // Keys that were just un-marked and need a {value:null, changed:true} reset signal.
        private readonly HashSet<string> pendingResets = new HashSet<string>();

        // Set to true once hydrated from persistence (SeedFrom or the explicit Mark-loop in Load()).
        // Never reset by ClearChanged()/Clear() — once seeded, always seeded.
        // BuildSettingsMap gates on this to avoid sending changed:false before Load() has run.
        private bool isSeeded;

        /// <inheritdoc/>
        public bool IsSeeded => isSeeded;

        /// <inheritdoc/>
        public bool IsChanged(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return changed.Contains(key) || PflagKeys.IsAlwaysChanged(key);
        }

        /// <inheritdoc/>
        public void Mark(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            // Always-changed keys are handled by IsAlwaysChanged(); adding them to the `changed`
            // set would cause them to appear in Snapshot() and get persisted unnecessarily.
            if (PflagKeys.IsAlwaysChanged(key)) return;
            changed.Add(key);
            // Cancel any pending reset for this key: the user has re-applied an override after
            // having previously reset to default. Without this, BuildSettingsMap would overwrite
            // the live {value:false, changed:true} entry with a Reset() signal (value:null).
            // Invariant: a key is never simultaneously in both `changed` and `pendingResets`.
            pendingResets.Remove(key);
        }

        /// <inheritdoc/>
        public void Unmark(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (changed.Remove(key))
                pendingResets.Add(key);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<string> ConsumePendingResets()
        {
            var result = pendingResets.ToList();
            pendingResets.Clear();
            return result;
        }

        /// <inheritdoc/>
        public void SeedFrom(IPersistableOptions options)
        {
            if (options == null) return;
            // Replace-semantics: clear BOTH changed and pendingResets before re-hydrating from
            // options. SeedFrom is a from-scratch operation (first load / upgrade path); any
            // pendingResets accumulated before seeding have no corresponding consumer and must
            // not survive into the first BuildSettingsMap call, where they would emit spurious
            // {value:null, changed:true} reset signals for keys the user never touched.
            Clear();
            foreach (var kv in GetGlobalKeyValues(options))
            {
                if (!ConfigDefaults.IsDefault(kv.Key, kv.Value))
                    Mark(kv.Key);
            }
            // Mark seeded after the loop so BuildSettingsMap's IsSeeded gate becomes active
            // once the tracker genuinely has data from persistence.
            isSeeded = true;
        }

        /// <inheritdoc/>
        public void ApplyUserEdits(IPersistableOptions options, IReadOnlyCollection<string> editedKeys)
        {
            if (options == null || editedKeys == null || editedKeys.Count == 0) return;

            // Build a lookup of the current key→value pairs for the global keys so we can check
            // each edited key without iterating the full set repeatedly.
            var currentValues = new Dictionary<string, object>();
            foreach (var kv in GetGlobalKeyValues(options))
                currentValues[kv.Key] = kv.Value;

            foreach (var key in editedKeys)
            {
                if (!currentValues.TryGetValue(key, out var value))
                    continue; // key not in global map — ignore (safe: unknown key has no tracker state)

                if (ConfigDefaults.IsDefault(key, value))
                    Unmark(key);  // user reset this key to default → unmark + enqueue reset
                else
                    Mark(key);    // user set this key to a non-default value → mark as override
            }
        }

        /// <inheritdoc/>
        public HashSet<string> Snapshot()
        {
            return new HashSet<string>(changed);
        }

        /// <inheritdoc/>
        public void MarkSeeded()
        {
            isSeeded = true;
        }

        /// <inheritdoc/>
        public void ClearChanged()
        {
            changed.Clear();
            // pendingResets is intentionally preserved: resets enqueued by ApplyUserEdits / Unmark
            // between saves must survive into the next BuildSettingsMap call to emit
            // {value:null, changed:true} reset signals.
            // isSeeded is intentionally NOT reset — once seeded, always seeded.
        }

        /// <inheritdoc/>
        public void Clear()
        {
            changed.Clear();
            pendingResets.Clear();
            // isSeeded is intentionally NOT reset — once seeded, always seeded.
        }

        // Returns the global (non-folder) pflag key → current value pairs from options.
        // This must mirror exactly the keys emitted by LsSettingsV25.BuildSettingsMap for global settings.
        private static IEnumerable<KeyValuePair<string, object>> GetGlobalKeyValues(IPersistableOptions options)
        {
            yield return Pair(PflagKeys.SnykOssEnabled, options.OssEnabled);
            yield return Pair(PflagKeys.SnykCodeEnabled, options.SnykCodeSecurityEnabled);
            yield return Pair(PflagKeys.SnykIacEnabled, options.IacEnabled);
            yield return Pair(PflagKeys.SnykSecretsEnabled, options.SecretsEnabled);

            // ScanAutomatic tracks the user's AutoScan preference. BuildSettingsMap sends
            // InternalAutoScan (which gates on additional runtime conditions), but the tracker
            // compares AutoScan (user intent) — intentional asymmetry.
            yield return Pair(PflagKeys.ScanAutomatic, options.AutoScan);
            yield return Pair(PflagKeys.ScanNetNew, options.EnableDeltaFindings);

            yield return Pair(PflagKeys.SeverityFilterCritical, options.FilterCritical);
            yield return Pair(PflagKeys.SeverityFilterHigh, options.FilterHigh);
            yield return Pair(PflagKeys.SeverityFilterMedium, options.FilterMedium);
            yield return Pair(PflagKeys.SeverityFilterLow, options.FilterLow);

            yield return Pair(PflagKeys.IssueViewOpenIssues, options.OpenIssuesEnabled);
            yield return Pair(PflagKeys.IssueViewIgnoredIssues, options.IgnoredIssuesEnabled);

            yield return Pair(PflagKeys.ApiEndpoint, options.CustomEndpoint ?? string.Empty);
            // Token and AuthenticationMethod must be tracked so the LS receives changed:true when
            // the user has set a non-default credential or auth mode.
            yield return Pair(PflagKeys.Token, options.ApiToken?.ToString() ?? string.Empty);
            yield return Pair(PflagKeys.AuthenticationMethod,
                options.AuthenticationMethod.ToString().ToLowerInvariant());
            yield return Pair(PflagKeys.Organization, options.Organization ?? string.Empty);
            yield return Pair(PflagKeys.ProxyInsecure, options.IgnoreUnknownCA);

            yield return Pair(PflagKeys.AutomaticDownload, options.BinariesAutoUpdate);
            // CliPath: tracker compares the raw options.CliCustomPath (user-configured path).
            // BuildSettingsMap sends the RESOLVED SnykCli.GetCliFilePath(...) — intentional.
            yield return Pair(PflagKeys.CliPath, options.CliCustomPath ?? string.Empty);
            yield return Pair(PflagKeys.CliReleaseChannel,
                options.CliReleaseChannel ?? SnykCliDownloader.DefaultReleaseChannel);
            yield return Pair(PflagKeys.BinaryBaseUrl,
                options.CliBaseDownloadURL ?? SnykCliDownloader.DefaultBaseDownloadUrl);

            yield return Pair(PflagKeys.AdditionalEnvironment, options.AdditionalEnv ?? string.Empty);
            yield return Pair(PflagKeys.AdditionalParameters,
                string.Join(" ", options.AdditionalParameters ?? new List<string>()));

            // RiskScoreThreshold: always yield (null == default == not set).
            // When HasValue is false we yield null so ApplyUserEdits can detect the transition
            // back to default and enqueue the pending reset signal.
            yield return Pair(PflagKeys.RiskScoreThreshold,
                options.RiskScoreThreshold.HasValue ? (object)options.RiskScoreThreshold.Value : null);
        }

        private static KeyValuePair<string, object> Pair(string key, object value)
            => new KeyValuePair<string, object>(key, value);

        /// <summary>
        /// Returns a snapshot of the global pflag key → current value pairs for
        /// <paramref name="options"/>. The snapshot has the same set of keys that
        /// <see cref="GetGlobalKeyValues"/> produces, using the same value normalisations.
        /// <para>
        /// Called by <see cref="HtmlSettingsScriptingBridge"/> to build a pre-apply snapshot,
        /// then compared against the post-apply <see cref="ISnykOptions"/> to derive the
        /// edit-delta for <see cref="ApplyUserEdits"/>.
        /// </para>
        /// </summary>
        internal static Dictionary<string, object> SnapshotGlobalKeys(IPersistableOptions options)
        {
            if (options == null) return new Dictionary<string, object>();
            var result = new Dictionary<string, object>();
            foreach (var kv in GetGlobalKeyValues(options))
                result[kv.Key] = kv.Value;
            return result;
        }

        /// <summary>
        /// Computes the set of pflag keys whose value in <paramref name="after"/> differs from
        /// the corresponding value in <paramref name="before"/>. Used by
        /// <see cref="HtmlSettingsScriptingBridge"/> to build the edit-delta passed to
        /// <see cref="ApplyUserEdits"/>.
        /// </summary>
        internal static IReadOnlyCollection<string> DiffGlobalKeys(
            Dictionary<string, object> before, IPersistableOptions after)
        {
            if (before == null || after == null) return new List<string>();
            var edited = new List<string>();
            var afterValues = SnapshotGlobalKeys(after);
            foreach (var kv in afterValues)
            {
                if (!before.TryGetValue(kv.Key, out var prev))
                {
                    edited.Add(kv.Key); // new key — treat as edited
                    continue;
                }
                // Compare by string representation to avoid boxing-equality issues with object.
                var prevStr = prev == null ? string.Empty : prev.ToString();
                var afterStr = kv.Value == null ? string.Empty : kv.Value.ToString();
                if (!string.Equals(prevStr, afterStr, StringComparison.Ordinal))
                    edited.Add(kv.Key);
            }
            return edited;
        }
    }
}
