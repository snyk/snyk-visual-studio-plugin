namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    using System;
    using CLI;
    using EnvDTE;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.Service;

    /// <summary>
    /// Service for solution settings.
    /// </summary>
    public class SnykUserStorageSettingsService
    {
        private static readonly ILogger Logger = LogManager.ForContext<SnykUserStorageSettingsService>();

        private readonly SnykSolutionService solutionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykUserStorageSettingsService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public SnykUserStorageSettingsService(ISnykServiceProvider serviceProvider)
        {
            this.solutionService = serviceProvider.SolutionService;
        }

        private SnykSettingsLoader SettingsLoader => new SnykSettingsLoader();

        /// <summary>
        /// Get CLI additional options string.
        /// </summary>
        /// <returns>string.</returns>
        public string GetAdditionalOptions()
        {
            Logger.Information("Enter GetAdditionalOptions method");

            string projectUniqueName = this.GetProjectUniqueName();

            Logger.Information($"Project unique name: {projectUniqueName}");

            if (string.IsNullOrEmpty(projectUniqueName))
            {
                Logger.Information("Project unique name is empty. Return from method");

                return string.Empty;
            }

            var settings = this.SettingsLoader.Load();

            if (settings == null || !settings.ProjectSettingsDict.ContainsKey(projectUniqueName))
            {
                return string.Empty;
            }
            else
            {
                return settings.ProjectSettingsDict[projectUniqueName].AdditionalOptions;
            }
        }

        /// <summary>
        /// Get is all projects enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool GetIsAllProjectsEnabled()
        {
            Logger.Information("Enter GetIsAllProjectsEnabled method");

            string projectUniqueName = this.GetProjectUniqueName();

            Logger.Information($"Project unique name: {projectUniqueName}");

            if (string.IsNullOrEmpty(projectUniqueName))
            {
                Logger.Information("Project unique name is empty. Return from method");

                return true;
            }

            var settings = this.SettingsLoader.Load();

            if (settings == null || !settings.ProjectSettingsDict.ContainsKey(projectUniqueName))
            {
                return true;
            }
            else
            {
                return settings.ProjectSettingsDict[projectUniqueName].IsAllProjectsScanEnabled;
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

            this.SettingsLoader.Save(settings);
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

            this.SettingsLoader.Save(settings);
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

            this.SettingsLoader.Save(settings);
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
        public bool IsUsageAnalyticsEnabled() => this.LoadSettings().UsageAnalyticsEnabled;

        /// <summary>
        /// Save usage analytics enabled option.
        /// </summary>
        /// <param name="usageAnalyticsEnabled">Bool param.</param>
        public void SaveUsageAnalyticsEnabled(bool usageAnalyticsEnabled)
        {
            var settings = this.LoadSettings();

            settings.UsageAnalyticsEnabled = usageAnalyticsEnabled;

            this.SettingsLoader.Save(settings);
        }

        /// <summary>
        /// Save Sentry anonymous user id.
        /// </summary>
        /// <param name="anonymousUserId">Anonymous user id to save.</param>
        public void SaveAnonymousId(string anonymousUserId)
        {
            var settings = this.SettingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            settings.AnonymousId = anonymousUserId;

            this.SettingsLoader.Save(settings);
        }

        /// <summary>
        /// Get Sentry anonymous user id.
        /// </summary>
        /// <returns>String Sentry anonymous user id.</returns>
        public string GetAnonymousId() => this.SettingsLoader.Load()?.AnonymousId;

        /// <summary>
        /// Get current CLI version.
        /// </summary>
        /// <returns>String in '1.100.1' format.</returns>
        public string GetCurrentCliVersion() => this.SettingsLoader.Load()?.CurrentCliVersion;

        /// <summary>
        /// Save CLI version number.
        /// </summary>
        /// <param name="cliVersion">CLI version to save.</param>
        public void SaveCurrentCliVersion(string cliVersion)
        {
            var settings = this.SettingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            settings.CurrentCliVersion = cliVersion;

            this.SettingsLoader.Save(settings);
        }

        /// <summary>
        /// Get current CLI version.
        /// </summary>
        /// <returns>String in '1.100.1' format.</returns>
        public DateTime GetCliReleaseLastCheckDate()
        {
            var checkDate = this.SettingsLoader.Load()?.CliReleaseLastCheckDate;

            return (DateTime)(checkDate == null ? DateTime.MinValue : checkDate);
        }

        /// <summary>
        /// Save last check date data.
        /// </summary>
        /// <param name="lastCheckDate">Last check date.</param>
        public void SaveCliReleaseLastCheckDate(DateTime lastCheckDate)
        {
            var settings = this.SettingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            settings.CliReleaseLastCheckDate = lastCheckDate;

            this.SettingsLoader.Save(settings);
        }

        /// <summary>
        /// Save additional options string.
        /// </summary>
        /// <param name="additionalOptions">CLI options string.</param>
        public void SaveAdditionalOptions(string additionalOptions)
        {
            Logger.Information("Enter SaveAdditionalOptions method");

            string projectUniqueName = this.GetProjectUniqueName();

            Logger.Information($"Project unique name: {projectUniqueName}");

            if (string.IsNullOrEmpty(projectUniqueName))
            {
                return;
            }

            var settings = this.SettingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            SnykProjectSettings projectSettings;

            if (settings.ProjectSettingsDict.ContainsKey(projectUniqueName))
            {
                projectSettings = settings.ProjectSettingsDict[projectUniqueName];
            }
            else
            {
                projectSettings = new SnykProjectSettings();
            }

            projectSettings.AdditionalOptions = additionalOptions;

            settings.ProjectSettingsDict[projectUniqueName] = projectSettings;

            this.SettingsLoader.Save(settings);

            Logger.Information("Leave SaveAdditionalOptions method");
        }

        /// <summary>
        /// Sace is all projects scan enabled.
        /// </summary>
        /// <param name="isAllProjectsEnabled">Bool param.</param>
        public void SaveIsAllProjectsScanEnabled(bool isAllProjectsEnabled)
        {
            Logger.Information("Enter SaveIsAllProjectsScan method");

            string projectUniqueName = this.GetProjectUniqueName();

            Logger.Information($"Project unique name: {projectUniqueName}");

            if (string.IsNullOrEmpty(projectUniqueName))
            {
                return;
            }

            var settings = this.SettingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            SnykProjectSettings projectSettings;

            if (settings.ProjectSettingsDict.ContainsKey(projectUniqueName))
            {
                projectSettings = settings.ProjectSettingsDict[projectUniqueName];
            }
            else
            {
                projectSettings = new SnykProjectSettings();
            }

            projectSettings.IsAllProjectsScanEnabled = isAllProjectsEnabled;

            settings.ProjectSettingsDict[projectUniqueName] = projectSettings;

            this.SettingsLoader.Save(settings);

            Logger.Information("Leave SaveIsAllProjectsScan method");
        }

        /// <summary>
        /// Get project unique name.
        /// </summary>
        /// <returns>Project name string.</returns>
        private string GetProjectUniqueName()
        {
            Logger.Information("Enter GetProjectUniqueName method");

            Projects projects = this.solutionService.GetProjects();

            if (projects.Count == 0)
            {
                return string.Empty;
            }

            Project project = projects.Item(1);

            Logger.Information($"Leave GetProjectUniqueName method. Project unique name: {project.UniqueName}");

            return project.UniqueName;
        }

        private SnykSettings LoadSettings()
        {
            var settings = this.SettingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            return settings;
        }
    }
}