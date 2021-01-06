﻿using EnvDTE;
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

        public SnykSolutionSettingsService(SnykSolutionService solutionService)
        {
            this.solutionService = solutionService;
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
            string projectUniqueName = GetProjectUniqueName();

            if (String.IsNullOrEmpty(projectUniqueName))
            {
                return "";
            }

            WritableSettingsStore settingsStore = GetUserSettingsStore();
                       
            if (!solutionService.IsSolutionOpen() || !settingsStore.CollectionExists(SnykProjectSettingsCollectionName))
            {
                return "";
            }

            string additionalOptions = "";

            try
            {
                additionalOptions = settingsStore.GetString(SnykProjectSettingsCollectionName, projectUniqueName);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            return additionalOptions;
        }

        public void SaveAdditionalOptions(string additionalOptions)
        {
            string projectUniqueName = GetProjectUniqueName();

            if (!String.IsNullOrEmpty(projectUniqueName))
            {
                if (String.IsNullOrEmpty(additionalOptions))
                {
                    additionalOptions = "";
                }

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
