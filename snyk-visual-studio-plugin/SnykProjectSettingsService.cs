using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Snyk.VisualStudio.Extension.Services;
using System;

namespace Snyk.VisualStudio.Extension.Settings
{
    class SnykProjectSettingsService
    {
        public const string SnykProjectSettingsCollectionName = "Snyk";

        private SnykSolutionService solutionService;

        public SnykProjectSettingsService(SnykSolutionService solutionService)
        {
            this.solutionService = solutionService;
        }

        public static SnykProjectSettingsService NewInstance(SnykSolutionService solutionService)
        {
            return new SnykProjectSettingsService(solutionService);
        }

        public bool IsProjectOpened()
        {
            return !String.IsNullOrEmpty(GetProjectUniqueName());
        }

        public string GetAdditionalOptions()
        {
            string projectUniqueName = GetProjectUniqueName();

            if (String.IsNullOrEmpty(projectUniqueName))
            {
                return "";
            }

            WritableSettingsStore settingsStore = GetUserSettingsStore();

            bool isProjectOpened = !String.IsNullOrEmpty(projectUniqueName);            

            if (!isProjectOpened || !settingsStore.CollectionExists(SnykProjectSettingsCollectionName))
            {
                return "";
            }

            return settingsStore.GetString(SnykProjectSettingsCollectionName, projectUniqueName);
        }

        public void SaveAdditionalOptions(string additionalOptions)
        {
            string projectUniqueName = GetProjectUniqueName();

            if (!String.IsNullOrEmpty(projectUniqueName))
            {
                GetUserSettingsStore().SetString(SnykProjectSettingsCollectionName, projectUniqueName, additionalOptions);
            }
        }

        public string GetProjectUniqueName()
        {
            Projects projects = solutionService.GetProjects();

            if (projects.Count == 0)
            {
                return "";
            }

            Project project = projects.Item(1);

            return project.UniqueName;
        }

        private WritableSettingsStore GetUserSettingsStore()
        {
            SettingsManager settingsManager = new ShellSettingsManager(solutionService.ServiceProvider);

            var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(SnykProjectSettingsCollectionName))
            {
                settingsStore.CreateCollection(SnykProjectSettingsCollectionName);
            }

            return settingsStore;
        }                                     
    }
}
