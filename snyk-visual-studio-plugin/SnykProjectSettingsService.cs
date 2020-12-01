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
            WritableSettingsStore settingsStore = GetUserSettingsStore();

            bool isProjectOpened = !String.IsNullOrEmpty(projectUniqueName);            

            if (!isProjectOpened || !settingsStore.CollectionExists(SnykProjectSettingsCollectionName))
            {
                return "";
            }

            return settingsStore.GetString(SnykProjectSettingsCollectionName, projectUniqueName);
        }

        public void saveAdditionalOptions(string additionalOptions)
        {
            string projectUniqueName = GetProjectUniqueName();

            if (!String.IsNullOrEmpty(projectUniqueName))
            {
                WritableSettingsStore settingsStore = GetUserSettingsStore();

                if (!settingsStore.CollectionExists(SnykProjectSettingsCollectionName))
                {
                    settingsStore.CreateCollection(SnykProjectSettingsCollectionName);
                }

                settingsStore.SetString(SnykProjectSettingsCollectionName, projectUniqueName, additionalOptions);
            }
        }

        public string GetProjectUniqueName()
        {
            DTE dte = (DTE)this.serviceProvider.GetService(typeof(DTE));
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

            return settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }                                     
    }
}
