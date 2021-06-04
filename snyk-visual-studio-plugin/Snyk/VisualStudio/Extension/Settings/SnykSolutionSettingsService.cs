namespace Snyk.VisualStudio.Extension.Settings
{
    using System;
    using System.Threading.Tasks;
    using EnvDTE;
    using CLI;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.Settings;

    /// <summary>
    /// Service for solution settings.
    /// </summary>
    public class SnykSolutionSettingsService
    {
        /// <summary>
        /// Main settings entry key name.
        /// </summary>
        public const string SnykSettingsCollectionName = "Snyk";

        /// <summary>
        /// Usage Analytics Enabled key name.
        /// </summary>
        public const string UsageAnalyticsEnabledName = "UsageAnalyticsEnabled";

        private readonly SnykSolutionService solutionService;

        private readonly SnykActivityLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykSolutionSettingsService"/> class.
        /// </summary>
        /// <param name="solutionService">Solution service intance.</param>
        public SnykSolutionSettingsService(SnykSolutionService solutionService)
        {
            this.solutionService = solutionService;

            this.logger = solutionService.Logger;
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

            var settingsStore = this.solutionService.ServiceProvider.SettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(SnykSettingsCollectionName))
            {
                settingsStore.CreateCollection(SnykSettingsCollectionName);
            }

            this.logger.LogInformation("Leave GetUserSettingsStore method");

            return settingsStore;
        }
    }
}