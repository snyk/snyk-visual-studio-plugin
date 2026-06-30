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
                Save(options, triggerSettingsChangedEvent: false);

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
            return new SnykOptions
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
        }

        public void Save(IPersistableOptions options, bool triggerSettingsChangedEvent = true)
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
