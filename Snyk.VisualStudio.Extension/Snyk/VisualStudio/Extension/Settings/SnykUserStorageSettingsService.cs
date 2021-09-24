namespace Snyk.VisualStudio.Extension.Settings
{
    using System;
    using System.Threading.Tasks;
    using EnvDTE;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.Settings;
    using Serilog;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.Service;

    /// <summary>
    /// Service for solution settings.
    /// </summary>
    public class SnykUserStorageSettingsService
    {
        /// <summary>
        /// Last date for check of CLI release.
        /// </summary>
        private const string CliReleaseLastCheckDateName = "CliReleaseLastCheckDate";

        /// <summary>
        /// Last date for check of CLI release.
        /// </summary>
        private const string CliReleaseLastCheckDateName = "CliReleaseLastCheckDate";

        /// <summary>
        /// Main settings entry key name.
        /// </summary>
        private const string SnykSettingsCollectionName = "Snyk";

        /// <summary>
        /// Usage Analytics Enabled key name.
        /// </summary>
        private const string UsageAnalyticsEnabledName = "UsageAnalyticsEnabled";

        /// <summary>
        /// Current CLI version key name.
        /// </summary>
        private const string CurrentCliVersionName = "CurrentCliVersionName";

        private const string OssEnabledName = "OssEnabled";

        private const string CodeSecurityEnabledName = "CodeSecurityEnabled";

        private const string CodeQualityEnabledName = "CodeQualityEnabled";

        private static readonly ILogger Logger = LogManager.ForContext<SnykUserStorageSettingsService>();

        private readonly SettingsManager settingsManager;

        private readonly SnykSolutionService solutionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnykUserStorageSettingsService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Snyk service provider.</param>
        public SnykUserStorageSettingsService(ISnykServiceProvider serviceProvider)
        {
            this.solutionService = serviceProvider.SolutionService;
            this.settingsManager = serviceProvider.SettingsManager;
        }

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

            WritableSettingsStore settingsStore = this.GetUserSettingsStore();

            if (!this.solutionService.IsSolutionOpen || !settingsStore.CollectionExists(SnykSettingsCollectionName))
            {
                Logger.Information($"Solution not open or {SnykSettingsCollectionName} collection not exists. Return from method.");

                return string.Empty;
            }

            string additionalOptions = string.Empty;

            try
            {
                Logger.Information("Try get additional options for project");

                additionalOptions = settingsStore.GetString(SnykSettingsCollectionName, projectUniqueName);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message);
            }

            return additionalOptions;
        }

        /// <summary>
        /// Get is all projects enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool GetIsAllProjectsEnabled()
        {
            Logger.Information("Enter GetIsAllProjectsEnabled method");

            bool isAllProjectsEnabled = true;

            string projectUniqueName = this.GetProjectUniqueName();

            Logger.Information($"Project unique name: {projectUniqueName}");

            if (string.IsNullOrEmpty(projectUniqueName))
            {
                Logger.Information("Project unique name is empty. Return from method");

                return isAllProjectsEnabled;
            }

            WritableSettingsStore settingsStore = this.GetUserSettingsStore();

            if (!this.solutionService.IsSolutionOpen || !settingsStore.CollectionExists(SnykSettingsCollectionName))
            {
                Logger.Information($"Solution not open or {SnykSettingsCollectionName} collection not exists. Return from method.");

                return isAllProjectsEnabled;
            }

            try
            {
                Logger.Information("Try get additional options for project");

                return settingsStore.GetBoolean(SnykSettingsCollectionName, projectUniqueName);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message);
            }

            return isAllProjectsEnabled;
        }

        /// <summary>
        /// Get usage analytics enabled.
        /// </summary>
        /// <returns>Bool.</returns>
        public bool GetUsageAnalyticsEnabled() => this.GetBoolValue(SnykSettingsCollectionName, UsageAnalyticsEnabledName, true);

        /// <summary>
        /// Save usage analytics enabled option.
        /// </summary>
        /// <param name="usageAnalyticsEnabled">Bool param.</param>
        public void SaveUsageAnalyticsEnabled(bool usageAnalyticsEnabled) =>
            this.SaveBoolValue(SnykSettingsCollectionName, UsageAnalyticsEnabledName, usageAnalyticsEnabled);

        /// <summary>
        /// Get oss enabled option.
        /// </summary>
        /// <returns>Oss enabled value.</returns>
        public bool GetOssEnabled() => this.GetBoolValue(SnykSettingsCollectionName, OssEnabledName, true);

        /// <summary>
        /// Get SnykCode security enabled option.
        /// </summary>
        /// <returns>SnykCode security enabled value.</returns>
        public bool GetSnykCodeSecurityEnabled() => this.GetBoolValue(SnykSettingsCollectionName, CodeSecurityEnabledName, true);

        /// <summary>
        /// Get SnykCode quality enabled option.
        /// </summary>
        /// <returns>SnykCode quality enabled value.</returns>
        public bool GetSnykCodeQualityEnabled() => this.GetBoolValue(SnykSettingsCollectionName, CodeQualityEnabledName, true);

        /// <summary>
        /// Save Oss enabled option.
        /// </summary>
        /// <param name="value">Oss enabled value.</param>
        public void SaveOssEnabled(bool value) => this.SaveBoolValue(SnykSettingsCollectionName, OssEnabledName, value);

        /// <summary>
        /// Save SnykCode security enabled option.
        /// </summary>
        /// <param name="value">SnykCode security enabled value.</param>
        public void SaveSnykCodeSecurityEnabled(bool value) => this.SaveBoolValue(SnykSettingsCollectionName, CodeSecurityEnabledName, value);

        /// <summary>
        /// Save SnykCode quality enabled option.
        /// </summary>
        /// <param name="value">SnykCode quality enabled value.</param>
        public void SaveSnykCodeQualityEnabled(bool value) => this.SaveBoolValue(SnykSettingsCollectionName, CodeQualityEnabledName, value);

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
                Logger.Error(exception.Message);
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
                Logger.Error(exception.Message);

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
            Logger.Information("Enter SaveAdditionalOptions method");

            string projectUniqueName = this.GetProjectUniqueName();

            Logger.Information($"Project unique name: {projectUniqueName}");

            if (!string.IsNullOrEmpty(projectUniqueName))
            {
                if (string.IsNullOrEmpty(additionalOptions))
                {
                    additionalOptions = string.Empty;
                }

                Logger.Information($"Save additional options for : {SnykSettingsCollectionName} collection");

                this.GetUserSettingsStore().SetString(SnykSettingsCollectionName, projectUniqueName, additionalOptions);
            }

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

            if (!string.IsNullOrEmpty(projectUniqueName))
            {
                Logger.Information($"Save is all projects enabled for : {SnykSettingsCollectionName} collection");

                this.GetUserSettingsStore().SetBoolean(SnykSettingsCollectionName, projectUniqueName, isAllProjectsEnabled);
            }

            Logger.Information("Leave SaveIsAllProjectsScan method");
        }

        /// <summary>
        /// Get project unique name.
        /// </summary>
        /// <returns>Project name string.</returns>
        public string GetProjectUniqueName()
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

        private static async Task<ShellSettingsManager> GetSettingsManagerAsync()
        {
            IServiceProvider serviceProvider = await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsSettingsManager)) as IServiceProvider;

            return new ShellSettingsManager(serviceProvider);
        }

        private WritableSettingsStore GetUserSettingsStore()
        {
            Logger.Information("Enter GetUserSettingsStore method");

            var settingsStore = this.settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(SnykSettingsCollectionName))
            {
                settingsStore.CreateCollection(SnykSettingsCollectionName);
            }

            Logger.Information("Leave GetUserSettingsStore method");

            return settingsStore;
        }

        private void SaveBoolValue(string collectionName, string propertyValue, bool value) =>
            this.GetUserSettingsStore().SetBoolean(collectionName, propertyValue, value);

        private bool GetBoolValue(string collectionName, string propertyName, bool defaultValue)
        {
            WritableSettingsStore settingsStore = this.GetUserSettingsStore();

            try
            {
                return settingsStore.GetBoolean(collectionName, propertyName);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message);
            }

            return defaultValue;
        }
    }
}