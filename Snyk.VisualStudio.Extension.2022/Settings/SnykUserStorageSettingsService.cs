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

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykUserStorageSettingsService"/> class.
        /// </summary>
        /// <param name="settingsPath">File path to settings file.</param>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public SnykUserStorageSettingsService(string settingsPath, ISnykServiceProvider serviceProvider)
        {
            this.solutionService = serviceProvider.SolutionService;

            this.settingsLoader = new SnykSettingsLoader(settingsPath);
        }

        public bool BinariesAutoUpdate
        {
            get => this.LoadSettings().BinariesAutoUpdateEnabled;
            set
            {
                var settings = this.LoadSettings();
                settings.BinariesAutoUpdateEnabled = value;
                this.settingsLoader.Save(settings);
            }
        }

        public string CliCustomPath
        {
            get => this.LoadSettings().CustomCliPath;
            set
            {
                var settings = this.LoadSettings();
                settings.CustomCliPath = value;
                this.settingsLoader.Save(settings);
            }
        }

        public AuthenticationType AuthenticationMethod
        {
            get => this.LoadSettings().AuthenticationMethod;
            set
            {
                var settings = this.LoadSettings();
                settings.AuthenticationMethod = value;
                this.settingsLoader.Save(settings);
            }
        }

        /// <summary>
        /// Gets or sets trusted folders list.
        /// </summary>
        public ISet<string> TrustedFolders
        {
            get => this.LoadSettings().TrustedFolders;
            set
            {
                var settings = this.LoadSettings();
                settings.TrustedFolders = value;
                this.settingsLoader.Save(settings);
            }
        }

        /// <summary>
        /// Get CLI additional options string.
        /// </summary>
        /// <returns>string.</returns>
        public async Task<string> GetAdditionalOptionsAsync()
        {
            Logger.Information("Enter GetAdditionalOptions method");

            int solutionPathHash = await this.GetSolutionPathHashAsync();

            var settings = this.settingsLoader.Load();

            if (settings == null || !settings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                return string.Empty;
            }

            return settings.SolutionSettingsDict[solutionPathHash].AdditionalOptions;
        }

        /// <summary>
        /// Get Auto Scan option
        /// </summary>
        /// <returns>bool.</returns>

        public bool AutoScan
        {
            get => this.LoadSettings().AutoScan;
            set
            {
                var settings = this.LoadSettings();
                settings.AutoScan = value;
                this.settingsLoader.Save(settings);
            }
        }

        /// <summary>
        /// Get Auto Scan option
        /// </summary>
        /// <returns>bool.</returns>

        public string Token
        {
            get => this.LoadSettings().Token;
            set
            {
                var settings = this.LoadSettings();
                settings.Token = value;
                this.settingsLoader.Save(settings);
            }
        }

        /// <summary>
        /// Get is all projects enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public async Task<bool> GetIsAllProjectsEnabledAsync()
        {
            Logger.Information("Enter GetIsAllProjectsEnabled method");

            int solutionPathHash = await this.GetSolutionPathHashAsync();

            var settings = this.settingsLoader.Load();

            if (settings == null || !settings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                return true;
            }
            else
            {
                return settings.SolutionSettingsDict[solutionPathHash].IsAllProjectsScanEnabled;
            }
        }

        /// <summary>
        /// Save snyk code security enalbed.
        /// </summary>
        /// <param name="enabled">Snyk code security enabled or disabled.</param>
        public void SaveSnykCodeSecurityEnabled(bool enabled)
        {
            var settings = this.LoadSettings();

            settings.SnykCodeSecurityEnabled = enabled;

            this.settingsLoader.Save(settings);
        }

        /// <summary>
        /// Get snyk code quality enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool IsSnykCodeQualityEnabled() => this.LoadSettings().SnykCodeQualityEnabled;

        /// <summary>
        /// Save snyk code quality enalbed.
        /// </summary>
        /// <param name="enabled">Snyk code quality enabled or disabled.</param>
        public void SaveSnykCodeQualityEnabled(bool enabled)
        {
            var settings = this.LoadSettings();

            settings.SnykCodeQualityEnabled = enabled;

            this.settingsLoader.Save(settings);
        }

        /// <summary>
        /// Get snyk code security enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool IsSnykCodeSecurityEnabled() => this.LoadSettings().SnykCodeSecurityEnabled;

        /// <summary>
        /// Save oss enabled.
        /// </summary>
        /// <param name="enabled">Enabled or disabled oss.</param>
        public void SaveOssEnabled(bool enabled)
        {
            var settings = this.LoadSettings();

            settings.OssEnabled = enabled;

            this.settingsLoader.Save(settings);
        }

        /// <summary>
        /// Get oss enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool IsOssEnabled() => this.LoadSettings().OssEnabled;

        /// <summary>
        /// Get usage analytics enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool IsErrorReportsEnabled() => this.LoadSettings().ErrorReportsEnabled;

        /// <summary>
        /// Save usage analytics enabled option.
        /// </summary>
        /// <param name="errorReportsEnabled">Bool param.</param>
        public void SaveErrorReportsEnabled(bool errorReportsEnabled)
        {
            var settings = this.LoadSettings();

            settings.ErrorReportsEnabled = errorReportsEnabled;

            this.settingsLoader.Save(settings);
        }

        /// <summary>
        /// Save Sentry anonymous user id.
        /// </summary>
        /// <param name="anonymousUserId">Anonymous user id to save.</param>
        public void SaveAnonymousId(string anonymousUserId)
        {
            var settings = this.settingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            settings.AnonymousId = anonymousUserId;

            this.settingsLoader.Save(settings);
        }

        /// <summary>
        /// Get Sentry anonymous user id.
        /// </summary>
        /// <returns>String anonymous user id.</returns>
        public string GetAnonymousId()
        {
            var snykSettings = settingsLoader.Load();
            if (snykSettings == null)
            {
                snykSettings = new SnykSettings();
            }

            if (string.IsNullOrEmpty(snykSettings.AnonymousId))
            {
                snykSettings.AnonymousId = System.Guid.NewGuid().ToString();
                settingsLoader.Save(snykSettings);
            }

            return snykSettings.AnonymousId;
        }

        /// <summary>
        /// Get current CLI version.
        /// </summary>
        /// <returns>String in '1.100.1' format.</returns>
        public string GetCurrentCliVersion() => this.settingsLoader.Load()?.CurrentCliVersion;

        /// <summary>
        /// Save CLI version number.
        /// </summary>
        /// <param name="cliVersion">CLI version to save.</param>
        public void SaveCurrentCliVersion(string cliVersion)
        {
            var settings = this.settingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            settings.CurrentCliVersion = cliVersion;

            this.settingsLoader.Save(settings);
        }

        /// <summary>
        /// Get current CLI version.
        /// </summary>
        /// <returns>String in '1.100.1' format.</returns>
        public DateTime GetCliReleaseLastCheckDate()
        {
            var checkDate = this.settingsLoader.Load()?.CliReleaseLastCheckDate;

            return (DateTime)(checkDate == null ? DateTime.MinValue : checkDate);
        }

        /// <summary>
        /// Save last check date data.
        /// </summary>
        /// <param name="lastCheckDate">Last check date.</param>
        public void SaveCliReleaseLastCheckDate(DateTime lastCheckDate)
        {
            var settings = this.settingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            settings.CliReleaseLastCheckDate = lastCheckDate;

            this.settingsLoader.Save(settings);
        }

        /// <summary>
        /// Save additional options string.
        /// </summary>
        /// <param name="additionalOptions">CLI options string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveAdditionalOptionsAsync(string additionalOptions)
        {
            Logger.Information("Enter SaveAdditionalOptions method");

            int solutionPathHash = await this.GetSolutionPathHashAsync();

            var settings = this.settingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            SnykSolutionSettings projectSettings;

            if (settings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                projectSettings = settings.SolutionSettingsDict[solutionPathHash];
            }
            else
            {
                projectSettings = new SnykSolutionSettings();
            }

            projectSettings.AdditionalOptions = additionalOptions;

            settings.SolutionSettingsDict[solutionPathHash] = projectSettings;

            this.settingsLoader.Save(settings);

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

            int solutionPathHash = await this.GetSolutionPathHashAsync();

            var settings = this.settingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            SnykSolutionSettings projectSettings;

            if (settings.SolutionSettingsDict.ContainsKey(solutionPathHash))
            {
                projectSettings = settings.SolutionSettingsDict[solutionPathHash];
            }
            else
            {
                projectSettings = new SnykSolutionSettings();
            }

            projectSettings.IsAllProjectsScanEnabled = isAllProjectsEnabled;

            settings.SolutionSettingsDict[solutionPathHash] = projectSettings;

            this.settingsLoader.Save(settings);

            Logger.Information("Leave SaveIsAllProjectsScan method");
        }

        private SnykSettings LoadSettings()
        {
            var settings = this.settingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
                this.settingsLoader.Save(settings);
            }

            return settings;
        }

        private async Task<int> GetSolutionPathHashAsync() =>
            (await this.solutionService.GetSolutionFolderAsync()).ToLower().GetHashCode();
    }
}