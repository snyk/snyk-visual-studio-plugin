namespace Snyk.VisualStudio.Extension.Settings
{
    using System;
    using System.Threading.Tasks;
    using EnvDTE;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.Settings;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Service for solution settings.
    /// </summary>
    public class SnykUserStorageSettingsService
    {
        /// <summary>
        /// Main settings entry key name.
        /// </summary>
        public const string SnykSettingsCollectionName = "Snyk";

        /// <summary>
        /// Usage Analytics Enabled key name.
        /// </summary>
        public const string UsageAnalyticsEnabledName = "UsageAnalyticsEnabled";

        /// <summary>
        /// Current CLI version key name.
        /// </summary>
        public const string CurrentCliVersionName = "CurrentCliVersionName";

        /// <summary>
        /// Last date for check of CLI release.
        /// </summary>
        public const string CliReleaseLastCheckDateName = "CliReleaseLastCheckDate";

        private readonly SettingsManager settingsManager;

        private readonly SnykActivityLogger logger;

        private readonly SnykSolutionService solutionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykUserStorageSettingsService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public SnykUserStorageSettingsService(ISnykServiceProvider serviceProvider)
        {
            this.solutionService = serviceProvider.SolutionService;
            this.settingsManager = serviceProvider.SettingsManager;
            this.logger = serviceProvider.ActivityLogger;
        }

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

            WritableSettingsStore settingsStore = this.GetUserSettingsStore();

            if (!this.solutionService.IsSolutionOpen || !settingsStore.CollectionExists(SnykSettingsCollectionName))
            {
                this.logger.LogInformation($"Solution not open or {SnykSettingsCollectionName} collection not exists. Return from method.");

                return string.Empty;
            }

            string additionalOptions = string.Empty;

            try
            {
                this.logger.LogInformation("Try get additional options for project");

                additionalOptions = settingsStore.GetString(SnykSettingsCollectionName, projectUniqueName);
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.Message);
            }

            return additionalOptions;
        }

        /// <summary>
        /// Get is all projects enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool GetIsAllProjectsEnabled()
        {
            this.logger.LogInformation("Enter GetIsAllProjectsEnabled method");

            bool isAllProjectsEnabled = true;

            string projectUniqueName = this.GetProjectUniqueName();

            this.logger.LogInformation($"Project unique name: {projectUniqueName}");

            if (string.IsNullOrEmpty(projectUniqueName))
            {
                this.logger.LogInformation("Project unique name is empty. Return from method");

                return isAllProjectsEnabled;
            }

            WritableSettingsStore settingsStore = this.GetUserSettingsStore();

            if (!this.solutionService.IsSolutionOpen || !settingsStore.CollectionExists(SnykSettingsCollectionName))
            {
                this.logger.LogInformation($"Solution not open or {SnykSettingsCollectionName} collection not exists. Return from method.");

                return isAllProjectsEnabled;
            }

            try
            {
                this.logger.LogInformation("Try get additional options for project");

                return settingsStore.GetBoolean(SnykSettingsCollectionName, projectUniqueName);
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.Message);
            }

            return isAllProjectsEnabled;
        }

        /// <summary>
        /// Get usage analytics enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool GetUsageAnalyticsEnabled()
        {
            bool usageAnalyticsEnabled = true;

            WritableSettingsStore settingsStore = this.GetUserSettingsStore();

            try
            {
                return settingsStore.GetBoolean(SnykSettingsCollectionName, UsageAnalyticsEnabledName);
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.Message);
            }

            return usageAnalyticsEnabled;
        }

        /// <summary>
        /// Save usage analytics enabled option.
        /// </summary>
        /// <param name="usageAnalyticsEnabled">Bool param.</param>
        public void SaveUsageAnalyticsEnabled(bool usageAnalyticsEnabled) => this.GetUserSettingsStore()
            .SetBoolean(SnykSettingsCollectionName, UsageAnalyticsEnabledName, usageAnalyticsEnabled);

        /// <summary>
        /// Get current CLI version.
        /// </summary>
        /// <returns>String in '1.100.1' format.</returns>
        public string GetCurrentCliVersion()
        {
            string cliVersion = string.Empty;

            try
            {
                cliVersion = this.GetUserSettingsStore().GetString(SnykSettingsCollectionName, CurrentCliVersionName);
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.Message);
            }

            return cliVersion;
        }

        /// <summary>
        /// Save CLI version number.
        /// </summary>
        /// <param name="cliVersion">CLI version to save.</param>
        public void SaveCurrentCliVersion(string cliVersion) => this.GetUserSettingsStore()
            .SetString(SnykSettingsCollectionName, CurrentCliVersionName, cliVersion);

        /// <summary>
        /// Get current CLI version.
        /// </summary>
        /// <returns>String in '1.100.1' format.</returns>
        public DateTime GetCliReleaseLastCheckDate()
        {
            DateTime dateTime;

            try
            {
                long tempDate = this.GetUserSettingsStore().GetInt64(SnykSettingsCollectionName, CliReleaseLastCheckDateName);

                dateTime = new DateTime(tempDate, DateTimeKind.Utc);
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.Message);

                dateTime = DateTime.MinValue;
            }

            return dateTime;
        }

        /// <summary>
        /// Save last check date data.
        /// </summary>
        /// <param name="lastCheckDate">Last check date.</param>
        public void SaveCliReleaseLastCheckDate(DateTime lastCheckDate) => this.GetUserSettingsStore()
            .SetInt64(SnykSettingsCollectionName, CliReleaseLastCheckDateName, lastCheckDate.Ticks);

        /// <summary>
        /// Save additional options string.
        /// </summary>
        /// <param name="additionalOptions">CLI options string.</param>
        public void SaveAdditionalOptions(string additionalOptions)
        {
            this.logger.LogInformation("Enter SaveAdditionalOptions method");

            string projectUniqueName = this.GetProjectUniqueName();

            this.logger.LogInformation($"Project unique name: {projectUniqueName}");

            if (!string.IsNullOrEmpty(projectUniqueName))
            {
                if (string.IsNullOrEmpty(additionalOptions))
                {
                    additionalOptions = string.Empty;
                }

                this.logger.LogInformation($"Save additional options for : {SnykSettingsCollectionName} collection");

                this.GetUserSettingsStore().SetString(SnykSettingsCollectionName, projectUniqueName, additionalOptions);
            }

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

            if (!string.IsNullOrEmpty(projectUniqueName))
            {
                this.logger.LogInformation($"Save is all projects enabled for : {SnykSettingsCollectionName} collection");

                this.GetUserSettingsStore().SetBoolean(SnykSettingsCollectionName, projectUniqueName, isAllProjectsEnabled);
            }

            this.logger.LogInformation("Leave SaveIsAllProjectsScan method");
        }

        /// <summary>
        /// Get project unique name.
        /// </summary>
        /// <returns>Project name string.</returns>
        public string GetProjectUniqueName()
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

        private static async Task<ShellSettingsManager> GetSettingsManagerAsync()
        {
            IServiceProvider serviceProvider = await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsSettingsManager)) as IServiceProvider;

            return new ShellSettingsManager(serviceProvider);
        }

        private WritableSettingsStore GetUserSettingsStore()
        {
            this.logger.LogInformation("Enter GetUserSettingsStore method");

            var settingsStore = this.settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(SnykSettingsCollectionName))
            {
                settingsStore.CreateCollection(SnykSettingsCollectionName);
            }

            this.logger.LogInformation("Leave GetUserSettingsStore method");

            return settingsStore;
        }
    }
}