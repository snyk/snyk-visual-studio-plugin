using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Threading;
using Snyk.VisualStudio.Extension.CLI;
using System;
using System.Threading.Tasks;

namespace Snyk.VisualStudio.Extension.Settings
{
    public class SnykSolutionSettingsService
    {
        public const string SnykSettingsCollectionName = "Snyk";

        public const string UsageAnalyticsEnabledName = "UsageAnalyticsEnabled";

        private readonly SnykSolutionService solutionService;

        private readonly SnykActivityLogger logger;

        public SnykSolutionSettingsService(SnykSolutionService solutionService)
        {
            this.solutionService = solutionService;

            this.logger = solutionService.Logger;
        }

        public string GetAdditionalOptions()
        {
            logger.LogInformation("Enter GetAdditionalOptions method");

            string projectUniqueName = GetProjectUniqueName();

            logger.LogInformation($"Project unique name: {projectUniqueName}");

            if (String.IsNullOrEmpty(projectUniqueName))
            {
                logger.LogInformation("Project unique name is empty. Return from method");

                return "";
            }

            WritableSettingsStore settingsStore = GetUserSettingsStore();
                       
            if (!solutionService.IsSolutionOpen || !settingsStore.CollectionExists(SnykSettingsCollectionName))
            {
                logger.LogInformation($"Solution not open or {SnykSettingsCollectionName} collection not exists. Return from method.");

                return "";
            }

            string additionalOptions = "";

            try
            {
                logger.LogInformation("Try get additional options for project");

                additionalOptions = settingsStore.GetString(SnykSettingsCollectionName, projectUniqueName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception.Message);
            }

            return additionalOptions;
        }

        public bool GetIsAllProjectsEnabled()
        {
            logger.LogInformation("Enter GetIsAllProjectsEnabled method");

            bool isAllProjectsEnabled = true;

            string projectUniqueName = GetProjectUniqueName();

            logger.LogInformation($"Project unique name: {projectUniqueName}");

            if (String.IsNullOrEmpty(projectUniqueName))
            {
                logger.LogInformation("Project unique name is empty. Return from method");

                return isAllProjectsEnabled;
            }

            WritableSettingsStore settingsStore = GetUserSettingsStore();

            if (!solutionService.IsSolutionOpen || !settingsStore.CollectionExists(SnykSettingsCollectionName))
            {
                logger.LogInformation($"Solution not open or {SnykSettingsCollectionName} collection not exists. Return from method.");

                return isAllProjectsEnabled;
            }            

            try
            {
                logger.LogInformation("Try get additional options for project");

                return settingsStore.GetBoolean(SnykSettingsCollectionName, projectUniqueName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception.Message);
            }

            return isAllProjectsEnabled;
        }

        public bool GetUsageAnalyticsEnabled()
        {
            bool usageAnalyticsEnabled = true;            

            WritableSettingsStore settingsStore = GetUserSettingsStore();

            try
            {
                return settingsStore.GetBoolean(SnykSettingsCollectionName, UsageAnalyticsEnabledName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception.Message);
            }

            return usageAnalyticsEnabled;
        }

        public void SaveUsageAnalyticsEnabled(bool usageAnalyticsEnabled) => GetUserSettingsStore()
            .SetBoolean(SnykSettingsCollectionName, UsageAnalyticsEnabledName, usageAnalyticsEnabled);

        public void SaveAdditionalOptions(string additionalOptions)
        {
            logger.LogInformation("Enter SaveAdditionalOptions method");

            string projectUniqueName = GetProjectUniqueName();

            logger.LogInformation($"Project unique name: {projectUniqueName}");

            if (!String.IsNullOrEmpty(projectUniqueName))
            {
                if (String.IsNullOrEmpty(additionalOptions))
                {
                    additionalOptions = "";
                }

                logger.LogInformation($"Save additional options for : {SnykSettingsCollectionName} collection");

                GetUserSettingsStore().SetString(SnykSettingsCollectionName, projectUniqueName, additionalOptions);
            }

            logger.LogInformation("Leave SaveAdditionalOptions method");
        }

        public void SaveIsAllProjectsScanEnabled(bool isAllProjectsEnabled)
        {
            logger.LogInformation("Enter SaveIsAllProjectsScan method");

            string projectUniqueName = GetProjectUniqueName();

            logger.LogInformation($"Project unique name: {projectUniqueName}");

            if (!String.IsNullOrEmpty(projectUniqueName))
            {                
                logger.LogInformation($"Save is all projects enabled for : {SnykSettingsCollectionName} collection");

                GetUserSettingsStore().SetBoolean(SnykSettingsCollectionName, projectUniqueName, isAllProjectsEnabled);
            }

            logger.LogInformation("Leave SaveIsAllProjectsScan method");
        }

        public string GetProjectUniqueName()
        {
            logger.LogInformation("Enter GetProjectUniqueName method");

            Projects projects = solutionService.GetProjects();

            if (projects.Count == 0)
            {
                return "";
            }

            Project project = projects.Item(1);

            logger.LogInformation($"Leave GetProjectUniqueName method. Project unique name: {project.UniqueName}");

            return project.UniqueName;
        }

        private static AsyncLazy<ShellSettingsManager> settingsManager = new AsyncLazy<ShellSettingsManager>(GetSettingsManagerAsync, ThreadHelper.JoinableTaskFactory);

        private static async Task<ShellSettingsManager> GetSettingsManagerAsync()
        {
            IServiceProvider serviceProvider = await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsSettingsManager)) as IServiceProvider;

            return new ShellSettingsManager(serviceProvider);
        }

        private WritableSettingsStore GetUserSettingsStore()
        {
            logger.LogInformation("Enter GetUserSettingsStore method");
            
            var settingsStore = solutionService.ServiceProvider.SettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(SnykSettingsCollectionName))
            {
                settingsStore.CreateCollection(SnykSettingsCollectionName);
            }

            logger.LogInformation("Leave GetUserSettingsStore method");

            return settingsStore;
        }        
    }    
}
