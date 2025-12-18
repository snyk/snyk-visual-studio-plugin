// ABOUTME: This file manages loading and saving Snyk settings from persistent storage
// ABOUTME: It handles serialization/deserialization of settings to file and provides solution-specific configuration management
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Service;
using System.Threading.Tasks;
using Serilog;

namespace Snyk.VisualStudio.Extension.Settings
{
    public class SnykOptionsManager : ISnykOptionsManager
    {
        private readonly ISnykServiceProvider serviceProvider;
        private readonly SnykSettingsLoader settingsLoader;
        private SnykSettings snykSettings;
        private static readonly ILogger Logger = LogManager.ForContext<SnykOptionsManager>();

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
                CliDownloadUrl = snykSettings.CliDownloadUrl,
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
            snykSettings.CliDownloadUrl = options.CliDownloadUrl;
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
        /// Get CLI additional options string.
        /// </summary>
        /// <returns>string.</returns>
        public async Task<string> GetAdditionalOptionsAsync()
        {
            Logger.Information("Enter GetAdditionalOptions method");

            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                return string.Empty;
            }

            return snykSettings.SolutionSettingsDict[solutionPathHash].AdditionalOptions;
        }

        /// <summary>
        /// Save additional options string.
        /// </summary>
        /// <param name="additionalOptions">CLI options string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveAdditionalOptionsAsync(string additionalOptions)
        {
            // TODO: Move to SnykOptionsManager
            Logger.Information("Enter SaveAdditionalOptions method");

            var solutionPathHash = await this.GetSolutionPathHashAsync();

            SnykSolutionSettings projectSettings;

            if (snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                projectSettings = snykSettings.SolutionSettingsDict[solutionPathHash];
            }
            else
            {
                projectSettings = new SnykSolutionSettings();
            }

            projectSettings.AdditionalOptions = additionalOptions;

            snykSettings.SolutionSettingsDict[solutionPathHash] = projectSettings;

            this.SaveSettingsToFile();

            Logger.Information("Leave SaveAdditionalOptions method");
        }

        /// <summary>
        /// Get additional environment variables string.
        /// </summary>
        /// <returns>string.</returns>
        public async Task<string> GetAdditionalEnvAsync()
        {
            Logger.Information("Enter GetAdditionalEnv method");

            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                return string.Empty;
            }

            return snykSettings.SolutionSettingsDict[solutionPathHash].AdditionalEnv;
        }

        /// <summary>
        /// Save additional environment variables string.
        /// </summary>
        /// <param name="additionalEnv">Environment variables string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveAdditionalEnvAsync(string additionalEnv)
        {
            Logger.Information("Enter SaveAdditionalEnv method");

            var solutionPathHash = await this.GetSolutionPathHashAsync();

            SnykSolutionSettings projectSettings;

            if (snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                projectSettings = snykSettings.SolutionSettingsDict[solutionPathHash];
            }
            else
            {
                projectSettings = new SnykSolutionSettings();
            }

            projectSettings.AdditionalEnv = additionalEnv;

            snykSettings.SolutionSettingsDict[solutionPathHash] = projectSettings;

            this.SaveSettingsToFile();

            Logger.Information("Leave SaveAdditionalEnv method");
        }

        /// <summary>
        /// Get organization string.
        /// </summary>
        /// <returns>string.</returns>
        public async Task<string> GetOrganizationAsync()
        {
            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                Logger.Information("Leave GetOrganization method");
                return string.Empty;
            }

            var organization = snykSettings.SolutionSettingsDict[solutionPathHash].Organization;
            return organization;
        }

        /// <summary>
        /// Save global organization string.
        /// </summary>
        /// <param name="organization">Organization string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SaveOrganizationAsync(string organization)
        {
            // Save to global organization setting (not solution-specific)
            // This matches Eclipse behavior where global org is always editable and used as fallback
            snykSettings.Organization = organization;
            this.SaveSettingsToFile();
            
            // Update the options object to reflect the change
            serviceProvider.Options.Organization = organization;
            return Task.CompletedTask;
        }


        /// <summary>           
        /// Get auto-determined organization.
        /// </summary>
        /// <returns>Auto-determined organization string.</returns>
        public async Task<string> GetAutoDeterminedOrgAsync()
        {
            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                return string.Empty;
            }

            var autoDeterminedOrg = snykSettings.SolutionSettingsDict[solutionPathHash].AutoDeterminedOrg;
            return autoDeterminedOrg ?? string.Empty;
        }

        /// <summary>
        /// Save auto-determined organization.
        /// </summary>
        /// <param name="autoDeterminedOrg">Auto-determined organization string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveAutoDeterminedOrgAsync(string autoDeterminedOrg)
        {
            var solutionPathHash = await this.GetSolutionPathHashAsync();

            SnykSolutionSettings projectSettings;

            if (snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                projectSettings = snykSettings.SolutionSettingsDict[solutionPathHash];
            }
            else
            {
                projectSettings = new SnykSolutionSettings();
            }

            projectSettings.AutoDeterminedOrg = autoDeterminedOrg;

            snykSettings.SolutionSettingsDict[solutionPathHash] = projectSettings;

            this.SaveSettingsToFile();
        }

        /// <summary>
        /// Get preferred organization.
        /// </summary>
        /// <returns>Preferred organization string.</returns>
        public async Task<string> GetPreferredOrgAsync()
        {
            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                return string.Empty;
            }

            var preferredOrg = snykSettings.SolutionSettingsDict[solutionPathHash].PreferredOrg;
            return preferredOrg ?? string.Empty;
        }

        /// <summary>
        /// Save preferred organization.
        /// </summary>
        /// <param name="preferredOrg">Preferred organization string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SavePreferredOrgAsync(string preferredOrg)
        {
            var solutionPathHash = await this.GetSolutionPathHashAsync();

            SnykSolutionSettings projectSettings;

            if (snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                projectSettings = snykSettings.SolutionSettingsDict[solutionPathHash];
            }
            else
            {
                projectSettings = new SnykSolutionSettings();
            }

            projectSettings.PreferredOrg = preferredOrg;

            snykSettings.SolutionSettingsDict[solutionPathHash] = projectSettings;

            this.SaveSettingsToFile();
        }

        /// <summary>
        /// Get organization set by user flag.
        /// </summary>
        /// <returns>Organization set by user flag.</returns>
        public async Task<bool> GetOrgSetByUserAsync()
        {
            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                return false; // Default to false (auto mode)
            }

            var orgSetByUser = snykSettings.SolutionSettingsDict[solutionPathHash].OrgSetByUser;
            return orgSetByUser;
        }

        /// <summary>
        /// Save organization set by user flag.
        /// </summary>
        /// <param name="orgSetByUser">Organization set by user flag.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveOrgSetByUserAsync(bool orgSetByUser)
        {
            var solutionPathHash = await this.GetSolutionPathHashAsync();

            SnykSolutionSettings projectSettings;

            if (snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                projectSettings = snykSettings.SolutionSettingsDict[solutionPathHash];
            }
            else
            {
                projectSettings = new SnykSolutionSettings();
            }

            projectSettings.OrgSetByUser = orgSetByUser;

            snykSettings.SolutionSettingsDict[solutionPathHash] = projectSettings;

            this.SaveSettingsToFile();
        }

        /// <summary>
        /// Get effective organization.
        /// </summary>
        /// <returns>Effective organization string.</returns>
        public async Task<string> GetEffectiveOrganizationAsync()
        {
            var solutionPathHash = await this.GetSolutionPathHashAsync();

            // Fallback hierarchy:
            // 1. Folder-specific values (highest priority)
            //    - autoDeterminedOrg when auto-detect is enabled
            //    - preferredOrg when manual mode is enabled
            // 2. Global organization setting (fallback)
            // 3. Empty string (final fallback)

            if (snykSettings != null && snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                var projectSettings = snykSettings.SolutionSettingsDict[solutionPathHash];
                
                // Check if organization was set by user (manual mode)
                if (projectSettings.OrgSetByUser)
                {
                    // Use preferredOrg when manual mode is enabled
                    if (!string.IsNullOrEmpty(projectSettings.PreferredOrg))
                    {
                        return projectSettings.PreferredOrg;
                    }
                }
                else
                {
                    // Use autoDeterminedOrg when auto-detect is enabled
                    if (!string.IsNullOrEmpty(projectSettings.AutoDeterminedOrg))
                    {
                        return projectSettings.AutoDeterminedOrg;
                    }
                }
            }

            // Fallback to global organization setting
            if (!string.IsNullOrEmpty(snykSettings?.Organization))
            {
                return snykSettings.Organization;
            }

            // Final fallback to empty string
            return string.Empty;
        }

        private async Task<int> GetSolutionPathHashAsync() =>
            (await this.serviceProvider.SolutionService.GetSolutionFolderAsync()).ToLower().GetHashCode();
    }
}
