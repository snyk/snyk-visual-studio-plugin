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
        /// Get organization string.
        /// </summary>
        /// <returns>string.</returns>
        public async Task<string> GetOrganizationAsync()
        {
            Logger.Information("Enter GetOrganization method");

            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                Logger.Information("Leave GetOrganization method");
                return string.Empty;
            }

            var organization = snykSettings.SolutionSettingsDict[solutionPathHash].Organization;
            Logger.Information("Leave GetOrganization method");
            return organization;
        }

        /// <summary>
        /// Save organization string.
        /// </summary>
        /// <param name="organization">Organization string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveOrganizationAsync(string organization)
        {
            Logger.Information("Enter SaveOrganization method");

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

            projectSettings.Organization = organization;

            snykSettings.SolutionSettingsDict[solutionPathHash] = projectSettings;

            this.SaveSettingsToFile();

            Logger.Information("Leave SaveOrganization method");
        }


        /// <summary>
        /// Get auto-determined organization.
        /// </summary>
        /// <returns>Auto-determined organization string.</returns>
        public async Task<string> GetAutoDeterminedOrgAsync()
        {
            Logger.Information("Enter GetAutoDeterminedOrg method");

            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                Logger.Information("Leave GetAutoDeterminedOrg method - no settings");
                return string.Empty;
            }

            var autoDeterminedOrg = snykSettings.SolutionSettingsDict[solutionPathHash].AutoDeterminedOrg;
            Logger.Information("Leave GetAutoDeterminedOrg method");
            return autoDeterminedOrg ?? string.Empty;
        }

        /// <summary>
        /// Save auto-determined organization.
        /// </summary>
        /// <param name="autoDeterminedOrg">Auto-determined organization string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveAutoDeterminedOrgAsync(string autoDeterminedOrg)
        {
            Logger.Information("Enter SaveAutoDeterminedOrg method");

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

            Logger.Information("Leave SaveAutoDeterminedOrg method");
        }

        /// <summary>
        /// Get preferred organization.
        /// </summary>
        /// <returns>Preferred organization string.</returns>
        public async Task<string> GetPreferredOrgAsync()
        {
            Logger.Information("Enter GetPreferredOrg method");

            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                Logger.Information("Leave GetPreferredOrg method - no settings");
                return string.Empty;
            }

            var preferredOrg = snykSettings.SolutionSettingsDict[solutionPathHash].PreferredOrg;
            Logger.Information("Leave GetPreferredOrg method");
            return preferredOrg ?? string.Empty;
        }

        /// <summary>
        /// Save preferred organization.
        /// </summary>
        /// <param name="preferredOrg">Preferred organization string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SavePreferredOrgAsync(string preferredOrg)
        {
            Logger.Information("Enter SavePreferredOrg method");

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

            Logger.Information("Leave SavePreferredOrg method");
        }

        /// <summary>
        /// Get organization set by user flag.
        /// </summary>
        /// <returns>Organization set by user flag.</returns>
        public async Task<bool> GetOrgSetByUserAsync()
        {
            Logger.Information("Enter GetOrgSetByUser method");

            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                Logger.Information("Leave GetOrgSetByUser method - using default");
                return false; // Default to false (auto mode)
            }

            var orgSetByUser = snykSettings.SolutionSettingsDict[solutionPathHash].OrgSetByUser;
            Logger.Information("Leave GetOrgSetByUser method");
            return orgSetByUser;
        }

        /// <summary>
        /// Save organization set by user flag.
        /// </summary>
        /// <param name="orgSetByUser">Organization set by user flag.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveOrgSetByUserAsync(bool orgSetByUser)
        {
            Logger.Information("Enter SaveOrgSetByUser method");

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

            Logger.Information("Leave SaveOrgSetByUser method");
        }

        /// <summary>
        /// Get effective organization based on IntelliJ logic.
        /// </summary>
        /// <returns>Effective organization string.</returns>
        public async Task<string> GetEffectiveOrganizationAsync()
        {
            Logger.Information("Enter GetEffectiveOrganization method");

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
                        Logger.Information("Using preferredOrg: {PreferredOrg}", projectSettings.PreferredOrg);
                        return projectSettings.PreferredOrg;
                    }
                }
                else
                {
                    // Use autoDeterminedOrg when auto-detect is enabled
                    if (!string.IsNullOrEmpty(projectSettings.AutoDeterminedOrg))
                    {
                        Logger.Information("Using autoDeterminedOrg: {AutoDeterminedOrg}", projectSettings.AutoDeterminedOrg);
                        return projectSettings.AutoDeterminedOrg;
                    }
                }
            }

            // Fallback to global organization setting
            if (!string.IsNullOrEmpty(snykSettings?.Organization))
            {
                Logger.Information("Using global organization: {GlobalOrg}", snykSettings.Organization);
                return snykSettings.Organization;
            }

            // Final fallback to empty string
            Logger.Information("Using empty string as final fallback");
            return string.Empty;
        }

        private async Task<int> GetSolutionPathHashAsync() =>
            (await this.serviceProvider.SolutionService.GetSolutionFolderAsync()).ToLower().GetHashCode();
    }
}
