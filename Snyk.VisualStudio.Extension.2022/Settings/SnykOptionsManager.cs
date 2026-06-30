// ABOUTME: This file manages loading and saving Snyk settings from persistent storage
// ABOUTME: It handles serialization/deserialization of settings to file and provides solution-specific configuration management
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
namespace Snyk.VisualStudio.Extension.Settings
{
    public class SnykOptionsManager : ISnykOptionsManager
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykOptionsManager>();

        private readonly ISnykServiceProvider serviceProvider;
        private readonly SnykSettingsLoader settingsLoader;
        private SnykSettings snykSettings;

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
            this.snykSettings = this.settingsLoader.Load();

            if (this.snykSettings != null) return;

            this.snykSettings = new SnykSettings();
            SaveSettingsToFile();
        }

        public void SaveSettingsToFile()
        {
            this.settingsLoader.Save(snykSettings);
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

                Logger.Information(
                    "Migrated legacy per-solution settings for '{Folder}' into a folder config.",
                    solutionFolderPath);
                return true;
            }
            catch (Exception e)
            {
                Logger.Warning(e, "Failed to migrate legacy per-solution settings for '{Folder}'.", solutionFolderPath);
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
            //   Seed once from value-vs-default (SeedFrom), then IMMEDIATELY persist both the
            //   resulting ChangedConfigKeys and the marker so the seed is authoritative even if
            //   the IDE crashes before a user-edit save. SeedFrom sets IsSeeded = true.
            //
            // Branch B — marker ABSENT + keys non-empty (migration: prior version wrote a set
            //   without the marker — treat as already-seeded, do NOT re-derive):
            //   Hydrate verbatim (Mark each key + MarkSeeded), then persist the marker so the
            //   next load follows Branch C and never re-derives.
            //
            // Branch C — marker PRESENT (steady state):
            //   The persisted set is the durable source of truth. Hydrate verbatim including an
            //   empty set, which means "seeded, zero user overrides." Never re-derive.
            if (!snykSettings.ChangedConfigKeysSeeded && (persistedKeys == null || persistedKeys.Count == 0))
            {
                // Branch A: true first run — seed from value-vs-default.
                // SeedFrom() performs a full Clear() (changed + pendingResets) before deriving marks,
                // so any pendingResets accumulated before this load are intentionally discarded here
                // (there is no prior user session to have enqueued them). The "pendingResets preserved
                // by ClearChanged()" note above applies to Branches B and C only.
                overrideTracker.SeedFrom(options);
                var seeded = overrideTracker.Snapshot();
                // ChangedConfigKeys is omitted from settings.json when null (NullValueHandling.Ignore
                // on the HashSet property), which happens when seeding finds zero non-default values.
                // ChangedConfigKeysSeeded (the bool marker) is omitted when false
                // (DefaultValueHandling.Ignore on the bool property) — it is now set to true so it
                // will be written, marking this file as seeded for all future loads (Branch C).
                snykSettings.ChangedConfigKeys = seeded.Count > 0 ? seeded : null;
                snykSettings.ChangedConfigKeysSeeded = true;
                SaveSettingsToFile();
            }
            else if (!snykSettings.ChangedConfigKeysSeeded)
            {
                // Branch B: marker absent but keys non-empty — prior-version migration.
                // Hydrate verbatim; persist the marker so next load uses Branch C.
                foreach (var key in persistedKeys)
                    overrideTracker.Mark(key);
                overrideTracker.MarkSeeded();
                snykSettings.ChangedConfigKeysSeeded = true;
                SaveSettingsToFile();
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

            // Write the seeded set back so it is available on the returned options object.
            options.ChangedConfigKeys = overrideTracker.Snapshot();
            return options;
        }

        public void Save(IPersistableOptions options, bool triggerSettingsChangedEvent = true,
                         bool updateOverrideTracker = true,
                         IReadOnlyCollection<string> editedKeys = null)
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
                var snapshot = overrideTracker.Snapshot();
                snykSettings.ChangedConfigKeys = snapshot.Count > 0
                    ? snapshot
                    : null; // keep out of settings.json when empty (NullValueHandling.Ignore)
            }
            // When updateOverrideTracker is false (LS/system-originated save): skip tracker
            // mutation so LS-pushed values (org, LDX flags, etc.) are never recorded as user
            // overrides. snykSettings.ChangedConfigKeys is intentionally left unchanged — the
            // user's persisted override set must survive every LS-push round-trip.

            this.SaveSettingsToFile();
            if(triggerSettingsChangedEvent)
                serviceProvider.Options.InvokeSettingsChangedEvent();
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
            snykSettings.Organization = organization;
            this.SaveSettingsToFile();
            serviceProvider.Options.Organization = organization;
            return Task.CompletedTask;
        }
    }
}
