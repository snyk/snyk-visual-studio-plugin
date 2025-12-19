// ABOUTME: This file defines the main interface for all Snyk settings and configuration options
// ABOUTME: It extends IPersistableOptions with runtime properties and the SettingsChanged event mechanism
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
        bool InternalAutoScan { get; set; }

        /// <summary>
        /// Settings changed event.
        /// </summary>
        event EventHandler<SnykSettingsChangedEventArgs> SettingsChanged;

        void InvokeSettingsChangedEvent();
    }
}