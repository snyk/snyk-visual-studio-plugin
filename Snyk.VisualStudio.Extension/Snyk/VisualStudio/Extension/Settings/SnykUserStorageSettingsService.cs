namespace Snyk.VisualStudio.Extension.Settings
{
    using System;
    using EnvDTE;
    using CLI;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Service for solution settings.
    /// </summary>
    public class SnykUserStorageSettingsService
    {
        private readonly SnykActivityLogger logger;

        private readonly SnykSolutionService solutionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykUserStorageSettingsService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public SnykUserStorageSettingsService(ISnykServiceProvider serviceProvider)
        {
            this.solutionService = serviceProvider.SolutionService;

            this.logger = serviceProvider.ActivityLogger;
        }

        private SnykSettingsLoader SettingsLoader => new SnykSettingsLoader { Logger = this.logger, };

        /// <summary>
        /// Get CLI additional options string.
        /// </summary>
        /// <returns>string.</returns>
        public string GetAdditionalOptions()
        {
            this.logger.LogInformation("Enter GetAdditionalOptions method");

            string projectUniqueName = this.GetProjectUniqueName();

            this.logger.LogInformation($"Project unique name: {projectUniqueName}");

            if (string.IsNullOrEmpty(projectUniqueName))
            {
                this.logger.LogInformation("Project unique name is empty. Return from method");

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
            this.logger.LogInformation("Enter GetIsAllProjectsEnabled method");

            string projectUniqueName = this.GetProjectUniqueName();

            this.logger.LogInformation($"Project unique name: {projectUniqueName}");

            if (string.IsNullOrEmpty(projectUniqueName))
            {
                this.logger.LogInformation("Project unique name is empty. Return from method");

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
        /// Get usage analytics enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool GetUsageAnalyticsEnabled()
        {
            var settings = this.SettingsLoader.Load();

            return settings == null ? true : settings.UsageAnalyticsEnabled;
        }

        /// <summary>
        /// Save usage analytics enabled option.
        /// </summary>
        /// <param name="usageAnalyticsEnabled">Bool param.</param>
        public void SaveUsageAnalyticsEnabled(bool usageAnalyticsEnabled)
        {
            var settings = this.SettingsLoader.Load();

            if (settings == null)
            {
                settings = new SnykSettings();
            }

            settings.UsageAnalyticsEnabled = usageAnalyticsEnabled;

            this.SettingsLoader.Save(settings);
        }

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
            this.logger.LogInformation("Enter SaveAdditionalOptions method");

            string projectUniqueName = this.GetProjectUniqueName();

            this.logger.LogInformation($"Project unique name: {projectUniqueName}");

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

            this.logger.LogInformation("Leave SaveAdditionalOptions method");
        }

        /// <summary>
        /// Sace is all projects scan enabled.
        /// </summary>
        /// <param name="isAllProjectsEnabled">Bool param.</param>
        public void SaveIsAllProjectsScanEnabled(bool isAllProjectsEnabled)
        {
            this.logger.LogInformation("Enter SaveIsAllProjectsScan method");

            string projectUniqueName = this.GetProjectUniqueName();

            this.logger.LogInformation($"Project unique name: {projectUniqueName}");

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

            this.logger.LogInformation("Leave SaveIsAllProjectsScan method");
        }

        /// <summary>
        /// Get project unique name.
        /// </summary>
        /// <returns>Project name string.</returns>
        private string GetProjectUniqueName()
        {
            this.logger.LogInformation("Enter GetProjectUniqueName method");

            Projects projects = this.solutionService.GetProjects();

            if (projects.Count == 0)
            {
                return string.Empty;
            }

            Project project = projects.Item(1);

            this.logger.LogInformation($"Leave GetProjectUniqueName method. Project unique name: {project.UniqueName}");

            return project.UniqueName;
        }
    }
}