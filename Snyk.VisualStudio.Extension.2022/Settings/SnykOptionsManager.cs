// ABOUTME: This file manages loading and saving Snyk settings from persistent storage
// ABOUTME: It handles serialization/deserialization of settings to file and provides solution-specific configuration management
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Service;
using System.Threading.Tasks;
namespace Snyk.VisualStudio.Extension.Settings
{
    public class SnykOptionsManager : ISnykOptionsManager
    {
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

                FolderConfigs = snykSettings.FolderConfigs,
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

            snykSettings.FolderConfigs = options.FolderConfigs;
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
