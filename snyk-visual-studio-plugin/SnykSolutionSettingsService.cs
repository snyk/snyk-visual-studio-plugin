using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Snyk.VisualStudio.Extension.Services;
using System;

namespace Snyk.VisualStudio.Extension.Settings
{
    public class SnykSolutionSettingsService
    {
        public const string SnykProjectSettingsCollectionName = "Snyk";

        private readonly SnykSolutionService solutionService;

        private readonly SnykActivityLogger logger;

        public SnykSolutionSettingsService(SnykSolutionService solutionService)
        {
            this.solutionService = solutionService;

            this.logger = solutionService.Logger;
        }

        public static SnykSolutionSettingsService NewInstance(SnykSolutionService solutionService)
        {
            return new SnykSolutionSettingsService(solutionService);
        }

        public bool IsProjectOpened()
        {
            return !String.IsNullOrEmpty(GetProjectUniqueName());
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
                       
            if (!solutionService.IsSolutionOpen() || !settingsStore.CollectionExists(SnykProjectSettingsCollectionName))
            {
                logger.LogInformation($"Solution not open or {SnykProjectSettingsCollectionName} collection not exists. Return from method.");

                return "";
            }

            string additionalOptions = "";

            try
            {
                logger.LogInformation("Try get additional options for project");

                additionalOptions = settingsStore.GetString(SnykProjectSettingsCollectionName, projectUniqueName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception.Message);
            }

            return additionalOptions;
        }

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

                logger.LogInformation($"Save additional options for : {SnykProjectSettingsCollectionName} collection");

                GetUserSettingsStore().SetString(SnykProjectSettingsCollectionName, projectUniqueName, additionalOptions);
            }

            logger.LogInformation("Leave SaveAdditionalOptions method");
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

        private WritableSettingsStore GetUserSettingsStore()
        {
            logger.LogInformation("Enter GetUserSettingsStore method");

            SettingsManager settingsManager = new ShellSettingsManager(solutionService.ServiceProvider);

            var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(SnykProjectSettingsCollectionName))
            {
                settingsStore.CreateCollection(SnykProjectSettingsCollectionName);
            }

            logger.LogInformation("Leave GetUserSettingsStore method");

            return settingsStore;
        }                                     
    }
}
