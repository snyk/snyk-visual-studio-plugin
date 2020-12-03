using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using System;

namespace Snyk.VisualStudio.Extension.Settings
{
    class SnykProjectSettingsService
    {
        public const string SnykProjectSettingsCollectionName = "Snyk";

        private IServiceProvider serviceProvider;

        public SnykProjectSettingsService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public static SnykProjectSettingsService NewInstance(IServiceProvider serviceProvider)
        {
            return new SnykProjectSettingsService(serviceProvider);
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
            DTE dte = (DTE)this.serviceProvider.GetService(typeof(DTE));

            if (dte == null)
            {
                return "";
            }

            Projects projects = dte.Solution.Projects;

            if (projects.Count == 0)
            {
                return "";
            }

            Project project = projects.Item(1);

            return project.UniqueName;
        }

        private WritableSettingsStore GetUserSettingsStore()
        {
            SettingsManager settingsManager = new ShellSettingsManager(serviceProvider);

            var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(SnykProjectSettingsCollectionName))
            {
                settingsStore.CreateCollection(SnykProjectSettingsCollectionName);
            }

            return settingsStore;
        }                                     
    }
}
