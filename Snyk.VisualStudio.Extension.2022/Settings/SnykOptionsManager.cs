// ABOUTME: This file manages loading and saving Snyk settings from persistent storage
// ABOUTME: It handles serialization/deserialization of settings to file and provides solution-specific configuration management
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Snyk.VisualStudio.Extension.Settings
{
    public class SnykOptionsManager : ISnykOptionsManager
    {
        // No static Logger field: LogManager.ForContext() depends on SnykDirectory (via the
        // Lazy<Logger> in LogManager). A static readonly field initialised at class-load time
        // risks re-entrancy if this class is loaded during that Lazy's construction.
        // Acquire the logger inline at each call site — identical to SnykDirectory and
        // SettingsLocationMigrator.

        private readonly ISnykServiceProvider serviceProvider;
        private readonly SnykSettingsLoader settingsLoader;
        private SnykSettings snykSettings;

        // True when the on-disk settings.json existed but could not be read/deserialised (corrupt or
        // mid-write) — as opposed to genuinely absent. Threaded from LoadSettingsFromFile (which gets
        // fileWasAbsent from SnykSettingsLoader.Load(out bool)) into Load(), where it hardens the
        // never-overwrite-a-recoverable-file guarantee (IDE-1483 FIX-D1) against the IDE-2152 seed
        // lifecycle: after a corrupt read snykSettings is a fresh unseeded SnykSettings() —
        // indistinguishable from a fresh install — so Load()'s Branch A would otherwise SeedFrom +
        // SaveSettingsToFile and clobber the recoverable token. When this is true, Load() seeds the
        // tracker IN MEMORY only and never writes to disk, leaving the recoverable file intact.
        private bool settingsFileWasUnreadable;

        // internal for testability (InternalsVisibleTo the test project): lets the corrupt-file tests
        // assert that a FAILED backup leaves the flag set (so a later Save retries the backup) and a
        // successful backup clears it. Not part of the public contract.
        internal bool SettingsFileWasUnreadableForTest => settingsFileWasUnreadable;

        // Serializes ALL settings-file persistence (IDE-2152 fix #4). Persisting callers still run on
        // different threads even after the DidChangeConfiguration reset-commit was marshaled to the UI
        // thread (IDE-2152 fix #7): the LS-push handlers in SnykLanguageClientCustomTarget
        // (OnSnykConfiguration, OnHasAuthenticated, OnAddTrustedFolders) call Save(updateOverrideTracker:
        // false) directly on StreamJsonRpc's BACKGROUND dispatch threads, concurrently with a UI-thread
        // user Save (clicks Apply). Both MUTATE the shared `snykSettings` object (its ChangedConfigKeys /
        // PendingResetConfigKeys HashSets — the UI-thread user Save does; the LS-push saves leave the sets
        // untouched but still write the object's other fields) and then SERIALIZE + WriteAllText the same
        // file. Guarding only the File.WriteAllText is not enough: one thread mutating a HashSet on
        // `snykSettings` while another serializes it throws "collection was modified", and two concurrent
        // writers tear the file. So the critical section is the whole mutate-snykSettings → serialize →
        // write region, taken by every persisting path. The lock is held only across synchronous work
        // (no `await` inside), so it can never deadlock a continuation. The UserOverrideTracker keeps its
        // own lock for its in-memory sets; this lock additionally makes the tracker-snapshot →
        // copy-into-snykSettings → write sequence atomic. This gate remains load-bearing (not merely
        // defense-in-depth) precisely because those LS-push background writers persist unchanged.
        private readonly object persistGate = new object();

        // Singleton tracker that lives alongside this manager (composed in the same root).
        // Exposed for injection into LsSettingsV25 via ISnykOptionsManager.OverrideTracker.
        private readonly UserOverrideTracker overrideTracker = new UserOverrideTracker();

        /// <summary>
        /// The user-override tracker owned by this manager. LsSettingsV25 uses this to set
        /// the <c>changed</c> flag on each ConfigSetting (IDE-2152).
        /// </summary>
        public IUserOverrideTracker OverrideTracker => overrideTracker;

        public SnykOptionsManager(string settingsFilePath, ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.settingsLoader = new SnykSettingsLoader(settingsFilePath);
            LoadSettingsFromFile();
        }

        public void LoadSettingsFromFile()
        {
            // Single-read discrimination (IDE-1483 FIX-D1): Load(out bool) performs ONE
            // File.ReadAllText and sets fileWasAbsent from the exception type, eliminating
            // the TOCTOU window that existed when a separate FileExists() probe was used.
            //   fileWasAbsent = true  => file genuinely not on disk (fresh install) — safe to write defaults.
            //   fileWasAbsent = false, result null => file exists but unreadable/corrupt
            //                                        — must NOT overwrite (token may be recoverable).
            this.snykSettings = this.settingsLoader.Load(out bool fileWasAbsent);

            if (this.snykSettings != null)
            {
                // File read + deserialised cleanly (absent or corrupt did not occur).
                this.settingsFileWasUnreadable = false;
                return;
            }

            // snykSettings == null: either genuinely absent OR present-but-unreadable/corrupt.
            //   fileWasAbsent == false here => the file exists but could not be read/deserialised.
            // Record that signal so Load() never persists over a recoverable file (IDE-1483 × IDE-2152).
            this.settingsFileWasUnreadable = !fileWasAbsent;

            this.snykSettings = new SnykSettings();
            if (fileWasAbsent)
                SaveSettingsToFile();
        }

        public void SaveSettingsToFile()
        {
            // Serialize + write under persistGate so two persisting threads never write the file
            // concurrently and never serialize `snykSettings` while another thread mutates its
            // HashSets (IDE-2152 fix #4). Reentrant: callers that already hold persistGate (Save,
            // CommitPendingResets) re-enter here safely, keeping their mutate+serialize+write atomic.
            lock (persistGate)
            {
                // Never-clobber-a-recoverable-file guard (IDE-1483 × IDE-2152, FIX-D2). After a
                // present-but-corrupt read (settingsFileWasUnreadable == true) snykSettings is a blank-
                // defaults object (Token="") — the Load-time fix keeps Load() from writing, but the FIRST
                // real Save (the LS-push Save(updateOverrideTracker:false) from SnykLanguageClientCustom-
                // Target, or CommitPendingResets / SaveOrganizationAsync / MigrateLegacySolutionSettings)
                // would serialize the blank object and overwrite the recoverable bytes → the auth token is
                // permanently lost. So on the FIRST write after a corrupt load, back the on-disk file up to
                // a sidecar BEFORE overwriting, so the recoverable token bytes are never destroyed. Then
                // clear the flag so subsequent saves in this session write normally (no repeated backups).
                if (settingsFileWasUnreadable)
                {
                    // A failed backup must BLOCK the destructive overwrite (IDE-1483 × IDE-2152,
                    // FIX-D2 hardening). BackupUnreadableSettingsFile returns TRUE when the backup
                    // succeeded OR there is nothing to protect (file absent/empty → safe to write);
                    // FALSE only when a real File.Copy of an existing non-empty file threw. On FALSE
                    // we must NOT clear the flag and must NOT overwrite: return early so the
                    // recoverable file survives and the NEXT Save retries the backup. Clearing the
                    // flag or writing here would overwrite the recoverable token with blank state and
                    // leave no backup — the exact loss this guard exists to prevent.
                    if (!BackupUnreadableSettingsFile())
                        return;
                    settingsFileWasUnreadable = false;
                }

                this.settingsLoader.Save(snykSettings);
            }
        }

        // Best-effort backup of a present-but-unreadable settings.json to a timestamped sidecar
        // (settings.json.corrupt-<UTC>.bak) before the first overwrite. Never THROWS (the File.Copy
        // is wrapped in a catch), but its RETURN VALUE now gates the destructive overwrite:
        //   TRUE  => the backup succeeded, OR there is nothing to protect (file absent or empty) —
        //            the caller may clear the unreadable flag and proceed with the write.
        //   FALSE => a real File.Copy of an existing NON-EMPTY file threw — the caller must NOT clear
        //            the flag and must NOT overwrite, so the recoverable token survives for the next
        //            Save to retry (SaveSettingsToFile returns early on FALSE).
        // Called only while holding persistGate. Uses CopyFile (a seam over File.Copy(overwrite:false))
        // with a unique timestamp so an existing backup is never clobbered.
        private bool BackupUnreadableSettingsFile()
        {
            try
            {
                var settingsFilePath = this.settingsLoader.SettingsFilePath;
                // Nothing to protect: no path, or the file is absent. Allow the write (it cannot lose
                // a recoverable token because there is no file to back up).
                if (string.IsNullOrEmpty(settingsFilePath) || !System.IO.File.Exists(settingsFilePath))
                    return true;

                // An empty (0-byte) on-disk file carries no recoverable bytes. Backing it up would
                // create a useless empty .bak, so treat it as nothing-to-protect and allow the write.
                if (new System.IO.FileInfo(settingsFilePath).Length == 0)
                    return true;

                var backupPath = settingsFilePath + ".corrupt-" +
                    DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".bak";
                CopyFile(settingsFilePath, backupPath);

                LogManager.ForContext(typeof(SnykOptionsManager)).Warning(
                    "settings.json was unreadable at load; backed it up to '{BackupPath}' before overwriting so a recoverable token is not lost.",
                    backupPath);
                return true;
            }
            catch (Exception e)
            {
                // A real backup failure of an existing non-empty file: block the overwrite so the
                // recoverable token is not destroyed. The caller keeps settingsFileWasUnreadable set
                // and skips this write; the next Save retries the backup.
                LogManager.ForContext(typeof(SnykOptionsManager)).Warning(e,
                    "Failed to back up an unreadable settings.json; skipping this overwrite to preserve the recoverable file.");
                return false;
            }
        }

        // Seam over File.Copy(overwrite:false) so tests can force a backup-copy failure
        // deterministically on any platform. Production behaviour is a plain File.Copy.
        protected virtual void CopyFile(string source, string destination)
        {
            System.IO.File.Copy(source, destination, overwrite: false);
        }

        /// <summary>
        /// One-time migration of legacy per-solution settings (IDE-1651). If the just-opened solution
        /// has an entry in the retained-but-dead <c>solutionSettingsDict</c>, fold it into the folder
        /// config for that path so it reaches the Language Server via the initialization options, then
        /// drop the legacy entry (and the whole section once empty). Best-effort and idempotent: a
        /// missing dict, a missing entry, or any failure is a no-op so it can never block LS startup.
        /// </summary>
        /// <param name="solutionFolderPath">The open solution folder (as returned by
        /// <c>ISolutionService.GetSolutionFolderAsync</c>).</param>
        /// <returns><c>true</c> if an entry was migrated.</returns>
        public bool MigrateLegacySolutionSettings(string solutionFolderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(solutionFolderPath))
                    return false;

                var legacy = snykSettings?.SolutionSettingsDict;
                if (legacy == null || legacy.Count == 0)
                    return false;

                var hash = LegacySolutionSettingsMigrator.ComputeFolderHash(solutionFolderPath);
                if (!legacy.TryGetValue(hash, out var entry) || entry == null)
                    return false;

                var options = serviceProvider.Options;
                var migrated = LegacySolutionSettingsMigrator.ToFolderConfig(entry, solutionFolderPath);
                options.FolderConfigs = LegacySolutionSettingsMigrator.Merge(options.FolderConfigs, migrated);

                legacy.Remove(hash);
                if (legacy.Count == 0)
                    snykSettings.SolutionSettingsDict = null;

                // Persist the seeded folder config plus the shrunken legacy section. No settings-changed
                // event: the caller is about to (re)start the LS, which receives these via the
                // initialization options, and Save leaves SolutionSettingsDict untouched so the shrink
                // is what gets written.
                // updateOverrideTracker:false — migration is system-driven, not a user override action;
                // calling ApplyUserEdits over migrated values would create phantom overrides.
                Save(options, triggerSettingsChangedEvent: false, updateOverrideTracker: false);

                LogManager.ForContext(typeof(SnykOptionsManager)).Information(
                    "Migrated legacy per-solution settings for '{Folder}' into a folder config.",
                    solutionFolderPath);
                return true;
            }
            catch (Exception e)
            {
                LogManager.ForContext(typeof(SnykOptionsManager)).Warning(e, "Failed to migrate legacy per-solution settings for '{Folder}'.", solutionFolderPath);
                return false;
            }
        }

        public ISnykOptions Load()
        {
            // Copy the persisted override set into options, or null when not present on disk.
            var persistedKeys = snykSettings.ChangedConfigKeys;

            var options = new SnykOptions
            {
                DeviceId = snykSettings.DeviceId,
                TrustedFolders = snykSettings.TrustedFolders,
                AnalyticsPluginInstalledSent = snykSettings.AnalyticsPluginInstalledSent,
                AutoScan = snykSettings.AutoScan,
                IgnoreUnknownCA = snykSettings.IgnoreUnknownCa,

                BinariesAutoUpdate = snykSettings.BinariesAutoUpdateEnabled,
                CliCustomPath = snykSettings.CustomCliPath,
                CliBaseDownloadURL = snykSettings.CliBaseDownloadURL,
                CliReleaseChannel = snykSettings.CliReleaseChannel,
                CurrentCliVersion = snykSettings.CurrentCliVersion,

                AuthenticationMethod = snykSettings.AuthenticationMethod,
                ApiToken = new AuthenticationToken(snykSettings.AuthenticationMethod, snykSettings.Token),
                CustomEndpoint = snykSettings.CustomEndpoint,
                Organization = snykSettings.Organization,

                // FolderConfigs are NOT loaded from disk: the LS is the source of truth and pushes
                // the full set via $/snyk.configuration after init (matching vscode/eclipse, which
                // never persist them). Start empty; the LS repopulates the in-memory list.
                FolderConfigs = new List<FolderConfig>(),
                EnableDeltaFindings = snykSettings.EnableDeltaFindings,

                OpenIssuesEnabled = snykSettings.OpenIssuesEnabled,
                IgnoredIssuesEnabled = snykSettings.IgnoredIssuesEnabled,

                IacEnabled = snykSettings.IacEnabled,
                SnykCodeSecurityEnabled = snykSettings.SnykCodeSecurityEnabled,
                OssEnabled = snykSettings.OssEnabled,
                SecretsEnabled = snykSettings.SecretsEnabled,

                FilterCritical = snykSettings.FilterCritical,
                FilterHigh = snykSettings.FilterHigh,
                FilterMedium = snykSettings.FilterMedium,
                FilterLow = snykSettings.FilterLow,

                AdditionalEnv = snykSettings.AdditionalEnv,
                AdditionalParameters = snykSettings.AdditionalParameters,
                RiskScoreThreshold = snykSettings.RiskScoreThreshold,
            };

            // Clear only the changed marks so a second Load() on the same manager instance never
            // unions stale marks from a previous load cycle. pendingResets is preserved: any reset
            // signals enqueued by ApplyUserEdits between saves must survive into the next BuildSettingsMap call.
            overrideTracker.ClearChanged();

            // Seeded-marker lifecycle (IDE-2152 refinement S) — three branches:
            //
            // Branch A — marker ABSENT + keys null/empty (true first run / fresh install):
            //   Seed once from value-vs-default (SeedFrom). SeedFrom sets IsSeeded = true.
            //
            // Branch B — marker ABSENT + keys non-empty (migration: prior version wrote a set
            //   without the marker — treat as already-seeded, do NOT re-derive):
            //   Hydrate verbatim (Mark each key + MarkSeeded).
            //
            // Branch C — marker PRESENT (steady state):
            //   The persisted set is the durable source of truth. Hydrate verbatim including an
            //   empty set, which means "seeded, zero user overrides." Never re-derive.
            //
            // DEFERRED-PERSIST (IDE-1483 × IDE-2152 merge fix): Branches A and B seed the tracker and
            // set the seeded marker + keys ON snykSettings IN MEMORY, but do NOT write to disk during
            // Load(). Persisting during Load would (1) overwrite a present-but-corrupt/unreadable
            // settings.json with blank defaults, permanently losing a recoverable auth token
            // (settingsFileWasUnreadable == true, snykSettings is a fresh unseeded object that looks
            // exactly like a fresh install → Branch A), and (2) rewrite a VALID-but-unmarked
            // (upgrading) file, breaking IDE-1483 D1-UNIT-003's "Load leaves a valid file
            // byte-unchanged" guarantee. Instead the seeded marker/keys are persisted lazily by the
            // next real Save (which serialises the whole snykSettings, marker included) or by
            // CommitPendingResets. Re-seeding in memory on each load before that first save is
            // idempotent: a fresh install re-derives the same empty/value-vs-default set every time,
            // and once a real Save runs the file gains the marker and all future loads take Branch C.
            if (!snykSettings.ChangedConfigKeysSeeded && (persistedKeys == null || persistedKeys.Count == 0))
            {
                // Branch A: true first run — seed from value-vs-default.
                // SeedFrom() performs a full Clear() (changed + pendingResets) before deriving marks,
                // so any pendingResets accumulated before this load are intentionally discarded here
                // (there is no prior user session to have enqueued them). The "pendingResets preserved
                // by ClearChanged()" note above applies to Branches B and C only.
                overrideTracker.SeedFrom(options);
                var seeded = overrideTracker.Snapshot();
                // Set the marker/keys ON snykSettings IN MEMORY only (no SaveSettingsToFile — see
                // DEFERRED-PERSIST above). A same-manager second Load() then observes the in-memory
                // marker and takes Branch C (no re-seed, no stale-mark unioning). Persistence is
                // deferred to the next real Save. Mutate under persistGate so a concurrent
                // Save/CommitPendingResets never reads a half-updated snykSettings.
                //
                // settingsFileWasUnreadable guard: after a present-but-corrupt read, snykSettings is a
                // fresh blank-defaults object that only LOOKS like a fresh install. Do NOT stamp the
                // seeded marker onto it in that case — leaving the marker unset keeps this blank object
                // from ever being mistaken for an authoritative seeded snapshot, so the recoverable
                // file is re-read and re-seeded fresh on the next process start. The tracker is still
                // seeded in memory (above) so this session behaves correctly.
                if (!settingsFileWasUnreadable)
                {
                    lock (persistGate)
                    {
                        snykSettings.ChangedConfigKeys = seeded.Count > 0 ? seeded : null;
                        snykSettings.ChangedConfigKeysSeeded = true;
                    }
                }
            }
            else if (!snykSettings.ChangedConfigKeysSeeded)
            {
                // Branch B: marker absent but keys non-empty — prior-version migration.
                // Hydrate verbatim; set the marker IN MEMORY so a same-manager next load uses Branch C.
                // Write is deferred to the next real Save (see DEFERRED-PERSIST above) so an upgrading
                // user's valid file is left byte-unchanged by Load().
                foreach (var key in persistedKeys)
                    overrideTracker.Mark(key);
                overrideTracker.MarkSeeded();
                lock (persistGate)
                {
                    snykSettings.ChangedConfigKeysSeeded = true;
                }
            }
            else
            {
                // Branch C: marker present — hydrate verbatim (may be null/empty; that is correct).
                foreach (var key in persistedKeys ?? System.Linq.Enumerable.Empty<string>())
                    overrideTracker.Mark(key);
                // MarkSeeded activates BuildSettingsMap's IsSeeded gate so it consults the
                // tracker rather than falling back to changed:true for every key.
                overrideTracker.MarkSeeded();
            }

            // Rehydrate persisted-but-unconfirmed pending resets (IDE-2152 fix #2) AFTER the changed
            // set has been hydrated by the branch above, so RehydratePendingResets can skip any key
            // that is now a live override mark (a persisted override means the user re-applied the key
            // after the reset was queued, so the override wins). Branch A (SeedFrom) clears the queue,
            // which is why this runs last. Skipped when the tracker was NOT hydrated from persistence
            // (Branch A on a genuinely fresh install writes no pending set; the disk field is null).
            overrideTracker.RehydratePendingResets(snykSettings.PendingResetConfigKeys);

            // Write the seeded set back so it is available on the returned options object.
            options.ChangedConfigKeys = overrideTracker.Snapshot();
            return options;
        }

        public void Save(IPersistableOptions options, bool triggerSettingsChangedEvent = true,
                         bool updateOverrideTracker = true,
                         IReadOnlyCollection<string> editedKeys = null,
                         IReadOnlyCollection<string> resetKeys = null)
        {
            // Hold persistGate across the ENTIRE mutate-snykSettings → tracker-apply → serialize →
            // write region (IDE-2152 fix #4) so a concurrent LS-push Save on a StreamJsonRpc background
            // dispatch thread (SnykLanguageClientCustomTarget.OnSnykConfiguration/OnHasAuthenticated/
            // OnAddTrustedFolders) can neither tear the file nor serialize `snykSettings` while this
            // thread mutates its HashSets. All work inside is synchronous — no `await` — so the lock is
            // safe to hold. The settings-changed event is fired AFTER the lock is released (below).
            lock (persistGate)
            {
            snykSettings.DeviceId = options.DeviceId;
            snykSettings.TrustedFolders = options.TrustedFolders;
            snykSettings.AnalyticsPluginInstalledSent = options.AnalyticsPluginInstalledSent;
            snykSettings.AutoScan = options.AutoScan;
            snykSettings.IgnoreUnknownCa = options.IgnoreUnknownCA;

            snykSettings.BinariesAutoUpdateEnabled = options.BinariesAutoUpdate;
            snykSettings.CustomCliPath = options.CliCustomPath;
            snykSettings.CliBaseDownloadURL = options.CliBaseDownloadURL;
            snykSettings.CliReleaseChannel = options.CliReleaseChannel;
            snykSettings.CurrentCliVersion = options.CurrentCliVersion;

            snykSettings.AuthenticationMethod = options.AuthenticationMethod;
            snykSettings.Token = options.ApiToken.ToString();

            snykSettings.CustomEndpoint = options.CustomEndpoint;
            snykSettings.Organization = options.Organization;

            // FolderConfigs are intentionally not persisted — the LS owns them and re-pushes after
            // init. Persisting would let stale folder overrides (and stale resets) survive a restart
            // and be re-sent, undoing the user's changes.
            snykSettings.EnableDeltaFindings = options.EnableDeltaFindings;

            snykSettings.OpenIssuesEnabled = options.OpenIssuesEnabled;
            snykSettings.IgnoredIssuesEnabled = options.IgnoredIssuesEnabled;

            snykSettings.IacEnabled = options.IacEnabled;
            snykSettings.SnykCodeSecurityEnabled = options.SnykCodeSecurityEnabled;
            snykSettings.OssEnabled = options.OssEnabled;
            snykSettings.SecretsEnabled = options.SecretsEnabled;

            snykSettings.FilterCritical = options.FilterCritical;
            snykSettings.FilterHigh = options.FilterHigh;
            snykSettings.FilterMedium = options.FilterMedium;
            snykSettings.FilterLow = options.FilterLow;

            snykSettings.AdditionalEnv = options.AdditionalEnv;
            snykSettings.AdditionalParameters = options.AdditionalParameters;
            snykSettings.RiskScoreThreshold = options.RiskScoreThreshold;

            if (updateOverrideTracker)
            {
                // User-initiated save: apply only the edit-delta (the keys the user actually
                // changed in this action) so org-pushed values sitting in Options are never
                // recorded as user overrides. editedKeys==null/empty → mark nothing (safe default).
                // Snapshot() returns a fresh copy; no need to wrap in another HashSet.
                overrideTracker.ApplyUserEdits(options, editedKeys ?? new List<string>());
                // Apply explicit resets (keys posted as JSON null by "Reset overrides"): un-mark the
                // local override AND enqueue the LS reset signal. Runs after ApplyUserEdits so the two
                // disjoint channels compose deterministically; the resulting Snapshot() below drops the
                // un-marked keys out of the persisted ChangedConfigKeys set (AC: "no longer marked as
                // changed"), which survives restart.
                overrideTracker.ApplyUserResets(resetKeys);
                var snapshot = overrideTracker.Snapshot();
                snykSettings.ChangedConfigKeys = snapshot.Count > 0
                    ? snapshot
                    : null; // keep out of settings.json when empty (NullValueHandling.Ignore)

                // Persist the pending-reset queue alongside ChangedConfigKeys (IDE-2152 fix #2): a
                // reset applied while the LS is not ready must survive a restart. Written even when
                // updateOverrideTracker is true only — the tracker is authoritative here. Mirrors the
                // ChangedConfigKeys null-when-empty convention (NullValueHandling.Ignore).
                PersistPendingResetsNoSave();
            }
            // When updateOverrideTracker is false (LS/system-originated save): skip tracker
            // mutation so LS-pushed values (org, LDX flags, etc.) are never recorded as user
            // overrides. snykSettings.ChangedConfigKeys is intentionally left unchanged — the
            // user's persisted override set must survive every LS-push round-trip.

            this.SaveSettingsToFile();
            } // release persistGate before firing the settings-changed event

            // Fired OUTSIDE persistGate: it is not persistence, and its handlers can run arbitrary
            // code (including paths that re-enter Save) — keeping it out of the lock avoids holding
            // the write lock across foreign callbacks.
            if(triggerSettingsChangedEvent)
                serviceProvider.Options.InvokeSettingsChangedEvent();
        }

        /// <summary>
        /// Commit reset keys that have been confirmed-delivered to the Language Server (IDE-2152 fix
        /// #2). Delegates to the tracker to remove exactly the sent keys from the in-memory queue, then
        /// re-persists the shrunken pending-reset set to disk so a delivered reset is not re-sent after
        /// a restart. This is the single commit entry point: the config-send path calls it (not the
        /// tracker directly) so persistence and the in-memory queue never diverge. No-op on null/empty.
        /// </summary>
        public void CommitPendingResets(IReadOnlyCollection<string> sentKeys)
        {
            if (sentKeys == null || sentKeys.Count == 0) return;
            overrideTracker.CommitPendingResets(sentKeys);
            // Hold persistGate across copy-into-snykSettings → write (IDE-2152 fix #4). The config-send
            // caller (DidChangeConfigurationAsync) now marshals to the UI thread before calling this
            // (IDE-2152 fix #7), so this no longer runs on the RPC continuation — but the gate is still
            // required: an LS-push Save on a StreamJsonRpc background dispatch thread
            // (SnykLanguageClientCustomTarget) may serialize the same `snykSettings` concurrently.
            // PersistPendingResetsNoSave mutates PendingResetConfigKeys and SaveSettingsToFile
            // serializes+writes — both must be inside the same critical section as Save's region. No
            // `await` inside; the tracker commit above already took (and released) the tracker's own
            // lock, so there is no lock-ordering hazard here.
            lock (persistGate)
            {
                PersistPendingResetsNoSave();
                this.SaveSettingsToFile();
            }
        }

        // Copies the tracker's current pending-reset queue into snykSettings.PendingResetConfigKeys,
        // using the null-when-empty convention (NullValueHandling.Ignore keeps the key out of
        // settings.json when nothing is pending). Does NOT write to disk — callers decide when to
        // flush (Save writes at its end; CommitPendingResets flushes explicitly).
        private void PersistPendingResetsNoSave()
        {
            var pending = overrideTracker.PeekPendingResets();
            snykSettings.PendingResetConfigKeys = pending.Count > 0
                ? new System.Collections.Generic.HashSet<string>(pending)
                : null;
        }

        /// <summary>
        /// Get organization string.
        /// </summary>
        /// <returns>string.</returns>
        public Task<string> GetOrganizationAsync()
        {
            return Task.FromResult(snykSettings?.Organization ?? string.Empty);
        }

        /// <summary>
        /// Save global organization string.
        /// </summary>
        /// <param name="organization">Organization string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SaveOrganizationAsync(string organization)
        {
            // Mutate snykSettings + write under persistGate (IDE-2152 fix #4) so this writer is
            // mutually exclusive with Save/CommitPendingResets.
            lock (persistGate)
            {
                snykSettings.Organization = organization;
                this.SaveSettingsToFile();
            }
            serviceProvider.Options.Organization = organization;
            return Task.CompletedTask;
        }
    }
}
