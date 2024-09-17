using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Snyk.Common;
using Snyk.Common.Authentication;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Service for solution settings.
    /// </summary>
    public class SnykUserStorageSettingsService : IUserStorageSettingsService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykUserStorageSettingsService>();

        private readonly ISolutionService solutionService;

        private readonly SnykSettingsLoader settingsLoader;

        private SnykSettings snykSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykUserStorageSettingsService"/> class.
        /// </summary>
        /// <param name="settingsPath">File path to settings file.</param>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public SnykUserStorageSettingsService(string settingsPath, ISnykServiceProvider serviceProvider)
        {
            this.solutionService = serviceProvider.SolutionService;

            this.settingsLoader = new SnykSettingsLoader(settingsPath);
            LoadSettings();
        }

        private void LoadSettings()
        {
            this.snykSettings = this.settingsLoader.Load();

            if (this.snykSettings == null)
            {
                this.snykSettings = new SnykSettings();
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            this.settingsLoader.Save(snykSettings);
        }

        public bool BinariesAutoUpdate
        {
            get => snykSettings.BinariesAutoUpdateEnabled;
            set => snykSettings.BinariesAutoUpdateEnabled = value;
        }

        public string CliCustomPath
        {
            get => snykSettings.CustomCliPath;
            set => snykSettings.CustomCliPath = value;
        }

        public AuthenticationType AuthenticationMethod
        {
            get => snykSettings.AuthenticationMethod;
            set => snykSettings.AuthenticationMethod = value;
        }

        /// <summary>
        /// Gets or sets trusted folders list.
        /// </summary>
        public ISet<string> TrustedFolders
        {
            get => snykSettings.TrustedFolders;
            set => snykSettings.TrustedFolders = value;
        }

        /// <summary>
        /// Get Auto Scan option
        /// </summary>
        /// <returns>bool.</returns>
        public bool AutoScan
        {
            get => snykSettings.AutoScan;
            set => snykSettings.AutoScan = value;
        }

        /// <summary>
        /// Get Or Set Auth Token
        /// </summary>
        /// <returns>string.</returns>
        public string Token
        {
            get => snykSettings.Token;
            set => snykSettings.Token = value;
        }

        public string CliReleaseChannel
        {
            get => snykSettings.CliReleaseChannel;
            set => snykSettings.CliReleaseChannel = value;
        }

        public string CliDownloadUrl
        {
            get => snykSettings.CliDownloadUrl;
            set => snykSettings.CliDownloadUrl = value;
        }

        public bool IgnoreUnknownCa
        {
            get => snykSettings.IgnoreUnknownCa;
            set => snykSettings.IgnoreUnknownCa = value;
        }

        public string Organization
        {
            get => snykSettings.Organization;
            set => snykSettings.Organization = value;
        }

        public string CustomEndpoint
        {
            get => snykSettings.CustomEndpoint;
            set => snykSettings.CustomEndpoint = value;
        }
        
        public bool SnykCodeSecurityEnabled
        {
            get => snykSettings.SnykCodeSecurityEnabled;
            set => snykSettings.SnykCodeSecurityEnabled = value;
        }

        public bool SnykCodeQualityEnabled
        {
            get => snykSettings.SnykCodeQualityEnabled;
            set => snykSettings.SnykCodeQualityEnabled = value;
        }

        public bool OssEnabled
        {
            get => snykSettings.OssEnabled;
            set => snykSettings.OssEnabled = value;
        }

        public bool IacEnabled
        {
            get => snykSettings.IacEnabled;
            set => snykSettings.IacEnabled = value;
        }

        public string CurrentCliVersion
        {
            get => snykSettings.CurrentCliVersion;
            set => snykSettings.CurrentCliVersion = value;
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

            this.SaveSettings();

            Logger.Information("Leave SaveAdditionalOptions method");
        }

        /// <summary>
        /// Sace is all projects scan enabled.
        /// </summary>
        /// <param name="isAllProjectsEnabled">Bool param.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveIsAllProjectsScanEnabledAsync(bool isAllProjectsEnabled)
        {
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

            this.SaveSettings();

            Logger.Information("Leave SaveIsAllProjectsScan method");
        }

        private async Task<int> GetSolutionPathHashAsync() =>
            (await this.solutionService.GetSolutionFolderAsync()).ToLower().GetHashCode();
    }
}