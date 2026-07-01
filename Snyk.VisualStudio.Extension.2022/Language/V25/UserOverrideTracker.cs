// ABOUTME: Tracks which global LSP pflag keys the user explicitly overrode from plugin defaults.
// ABOUTME: Persists via SnykSettings.ChangedConfigKeys; seeds from options on first load (IDE-2152).
using System.Collections.Generic;
using System.Linq;
using Snyk.VisualStudio.Extension.Download;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Language
{
    /// <inheritdoc cref="IUserOverrideTracker"/>
    public class UserOverrideTracker : IUserOverrideTracker
    {
        // Single lock guarding ALL access to `changed`, `pendingResets`, and `isSeeded`.
        // The tracker is mutated from the UI thread (SnykOptionsManager.Save →
        // ApplyUserEdits/ApplyUserResets) AND from thread-pool continuations (the fire-and-forget
        // config-send path commits after `await ...ConfigureAwait(false)`), from multiple call
        // sites. A non-thread-safe HashSet mutated concurrently can throw or corrupt state, so every
        // public read and mutation takes this lock. Public methods must never call each other while
        // holding the lock except through the *NoLock helpers below (C# locks are reentrant, but the
        // explicit split keeps the atomic boundary obvious). Peek/Snapshot copy under the lock so the
        // returned collection can never be observed mid-mutation.
        private readonly object gate = new object();

        // Keys that the user has explicitly set away from plugin defaults.
        private readonly HashSet<string> changed = new HashSet<string>();

        // Keys that were just un-marked and need a {value:null, changed:true} reset signal.
        private readonly HashSet<string> pendingResets = new HashSet<string>();

        // Set to true once hydrated from persistence (SeedFrom or the explicit Mark-loop in Load()).
        // Never reset by ClearChanged()/Clear() — once seeded, always seeded.
        // BuildSettingsMap gates on this to avoid sending changed:false before Load() has run.
        private bool isSeeded;

        /// <inheritdoc/>
        public bool IsSeeded
        {
            get { lock (gate) return isSeeded; }
        }

        /// <inheritdoc/>
        public bool IsChanged(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            // IsAlwaysChanged reads an immutable static set — safe outside the lock; but `changed`
            // must be read under the lock.
            if (PflagKeys.IsAlwaysChanged(key)) return true;
            lock (gate) return changed.Contains(key);
        }

        /// <inheritdoc/>
        public void Mark(string key)
        {
            lock (gate) MarkNoLock(key);
        }

        // Core Mark logic; caller must hold `gate`.
        private void MarkNoLock(string key)
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
            lock (gate)
            {
                if (changed.Remove(key))
                    pendingResets.Add(key);
            }
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<string> PeekPendingResets()
        {
            // Non-destructive: return a copy TAKEN UNDER THE LOCK so the caller can iterate while the
            // queue stays intact until CommitPendingResets confirms delivery (IDE-2152 CP 2.2). The
            // copy also isolates the caller from concurrent mutation of the live set.
            lock (gate) return pendingResets.ToList();
        }

        /// <inheritdoc/>
        public void CommitPendingResets(IReadOnlyCollection<string> sentKeys)
        {
            if (sentKeys == null) return;
            lock (gate)
            {
                // Remove only the keys that were actually delivered. Never a blanket Clear(): a reset
                // for a DIFFERENT key enqueued between the peek (what was sent) and this commit was
                // never in the confirmed message, so it must survive to be re-delivered on the next
                // config update.
                foreach (var key in sentKeys)
                    pendingResets.Remove(key);
            }
        }

        /// <inheritdoc/>
        public void ApplyUserResets(IReadOnlyCollection<string> resetKeys)
        {
            if (resetKeys == null || resetKeys.Count == 0) return;
            lock (gate)
            {
                foreach (var key in resetKeys)
                {
                    if (string.IsNullOrEmpty(key)) continue;
                    // Best-effort local un-mark: drop the override so the key leaves Snapshot() (and
                    // thus the persisted ChangedConfigKeys set). Remove returns false when there was
                    // no mark — that is fine, we still enqueue the LS reset signal below.
                    changed.Remove(key);
                    // Always enqueue the reset signal, even when there was no local mark: a form reset
                    // must tell the LS to Unset any user:global override (e.g. an org-pushed value the
                    // user wants cleared). This is the key difference from Unmark, which only enqueues
                    // when the key was previously in the changed set.
                    pendingResets.Add(key);
                }
            }
        }

        /// <inheritdoc/>
        public void RehydratePendingResets(IReadOnlyCollection<string> resetKeys)
        {
            if (resetKeys == null || resetKeys.Count == 0) return;
            lock (gate)
            {
                foreach (var key in resetKeys)
                {
                    if (string.IsNullOrEmpty(key)) continue;
                    // Re-queue a persisted-but-unconfirmed reset on Load() so it is re-delivered after
                    // a restart. Mirror the ApplyUserResets invariant: a rehydrated reset must never
                    // coexist with a live override mark for the same key (a persisted override wins —
                    // it means the user re-applied the key after the reset was queued).
                    if (!changed.Contains(key))
                        pendingResets.Add(key);
                }
            }
        }

        /// <inheritdoc/>
        public void SeedFrom(IPersistableOptions options)
        {
            if (options == null) return;
            lock (gate)
            {
                // Replace-semantics: clear BOTH changed and pendingResets before re-hydrating from
                // options. SeedFrom is a from-scratch operation (first load / upgrade path); any
                // pendingResets accumulated before seeding have no corresponding consumer and must
                // not survive into the first BuildSettingsMap call, where they would emit spurious
                // {value:null, changed:true} reset signals for keys the user never touched.
                changed.Clear();
                pendingResets.Clear();
                foreach (var kv in GetGlobalKeyValues(options))
                {
                    if (!ConfigDefaults.IsDefault(kv.Key, kv.Value))
                        MarkNoLock(kv.Key);
                }
                // Mark seeded after the loop so BuildSettingsMap's IsSeeded gate becomes active
                // once the tracker genuinely has data from persistence.
                isSeeded = true;
            }
        }

        /// <inheritdoc/>
        public void ApplyUserEdits(IPersistableOptions options, IReadOnlyCollection<string> editedKeys)
        {
            if (editedKeys == null || editedKeys.Count == 0) return;

            // Every key the settings form posted (present in editedKeys) is an explicit user choice
            // and is recorded as an override — regardless of whether its value happens to equal the
            // plugin default. This fixes the "enabling Snyk Code doesn't persist" bug (PR #515):
            // Snyk Code's default is `true`, so a user *enabling* it posts a value == the default;
            // inferring a reset from value==default here turned the enable into a
            // reset-to-org-default signal and let the org value silently win.
            //
            // Reset-to-default is henceforth an explicit user action only (via Unmark), never
            // inferred from the value equalling the default. Mark() already no-ops on null/empty
            // keys and on IsAlwaysChanged keys, and cancels any pending reset for the key.
            lock (gate)
            {
                foreach (var key in editedKeys)
                    MarkNoLock(key);
            }
        }

        /// <inheritdoc/>
        public HashSet<string> Snapshot()
        {
            lock (gate) return new HashSet<string>(changed);
        }

        /// <inheritdoc/>
        public void MarkSeeded()
        {
            lock (gate) isSeeded = true;
        }

        /// <inheritdoc/>
        public void ClearChanged()
        {
            lock (gate) changed.Clear();
            // pendingResets is intentionally preserved: resets enqueued by ApplyUserEdits / Unmark
            // between saves must survive into the next BuildSettingsMap call to emit
            // {value:null, changed:true} reset signals.
            // isSeeded is intentionally NOT reset — once seeded, always seeded.
        }

        /// <inheritdoc/>
        public void Clear()
        {
            lock (gate)
            {
                changed.Clear();
                pendingResets.Clear();
            }
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
            // When HasValue is false we yield null so SeedFrom compares "unset" against the
            // default and does not mark the key as an override. GetGlobalKeyValues is consumed
            // only by SeedFrom; ApplyUserEdits no longer reads it (it marks every edited key
            // verbatim — reset-to-default is an explicit user action, never inferred here).
            yield return Pair(PflagKeys.RiskScoreThreshold,
                options.RiskScoreThreshold.HasValue ? (object)options.RiskScoreThreshold.Value : null);
        }

        private static KeyValuePair<string, object> Pair(string key, object value)
            => new KeyValuePair<string, object>(key, value);
    }
}
