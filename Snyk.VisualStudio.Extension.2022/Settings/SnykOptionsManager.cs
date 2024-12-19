using System.IO;
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
                SnykCodeQualityEnabled = snykSettings.SnykCodeQualityEnabled,
                SnykCodeSecurityEnabled = snykSettings.SnykCodeSecurityEnabled,
                OssEnabled = snykSettings.OssEnabled,
            };
        }

        public void Save(IPersistableOptions options)
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
            snykSettings.SnykCodeQualityEnabled = options.SnykCodeQualityEnabled;
            snykSettings.SnykCodeSecurityEnabled = options.SnykCodeSecurityEnabled;
            snykSettings.OssEnabled = options.OssEnabled;

            this.SaveSettingsToFile();
            serviceProvider.Options.InvokeSettingsChangedEvent();
        }

        /// <summary>
        /// Get is all projects enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public async Task<bool> GetIsAllProjectsEnabledAsync()
        {
            Logger.Information("Enter GetIsAllProjectsEnabled method");

            var solutionPathHash = await this.GetSolutionPathHashAsync();

            if (snykSettings == null || !snykSettings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                return true;
            }
            else
            {
                return snykSettings.SolutionSettingsDict[solutionPathHash].IsAllProjectsScanEnabled;
            }
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
        /// Sace is all projects scan enabled.
        /// </summary>
        /// <param name="isAllProjectsEnabled">Bool param.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveIsAllProjectsScanEnabledAsync(bool isAllProjectsEnabled)
        {
            // TODO: Move to SnykOptionsManager
            Logger.Information("Enter SaveIsAllProjectsScan method");

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

            projectSettings.IsAllProjectsScanEnabled = isAllProjectsEnabled;

            snykSettings.SolutionSettingsDict[solutionPathHash] = projectSettings;

            this.SaveSettingsToFile();

            Logger.Information("Leave SaveIsAllProjectsScan method");
        }

        private async Task<int> GetSolutionPathHashAsync() =>
            (await this.serviceProvider.SolutionService.GetSolutionFolderAsync()).ToLower().GetHashCode();
    }
}
