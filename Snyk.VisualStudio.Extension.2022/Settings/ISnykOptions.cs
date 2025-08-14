using System;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Interface for Snyk Options/Settings in Visual Studio.
    /// </summary>
    public interface ISnykOptions : IPersistableOptions
    {
        string Application { get; set; }
        string ApplicationVersion { get; set; }
        string IntegrationName { get; }
        string IntegrationVersion { get; }
        string IntegrationEnvironment { get; set; }
        string IntegrationEnvironmentVersion { get; set; }
        bool ConsistentIgnoresEnabled { get; set; }
        public bool InternalAutoScan { get; set; }

        /// <summary>
        /// Settings changed event.
        /// </summary>
        event EventHandler<SnykSettingsChangedEventArgs> SettingsChanged;

        void InvokeSettingsChangedEvent();
    }
}