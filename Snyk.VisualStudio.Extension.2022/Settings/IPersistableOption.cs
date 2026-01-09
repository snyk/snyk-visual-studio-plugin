// ABOUTME: This file defines the interface for Snyk options that can be persisted to disk storage
// ABOUTME: It contains all configuration properties that need to be saved and restored across VS sessions
using System.Collections.Generic;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Settings;

public interface IPersistableOptions
{
    string DeviceId { get; set; }
    bool AutoScan { get; set; }

    bool OpenIssuesEnabled { get; set; }
    bool IgnoredIssuesEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Snyk user API token.
    /// </summary>
    AuthenticationToken ApiToken { get; set; }

    /// <summary>
    /// Gets Value of Authentication Token Type.
    /// </summary>
    AuthenticationType AuthenticationMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether CLI custom endpoint parameter.
    /// </summary>
    string CustomEndpoint { get; set; }

    /// <summary>
    /// Gets a value indicating whether Snyk Code settings URL.
    /// </summary>
    string SnykCodeSettingsUrl { get; }

    /// <summary>
    /// Gets or sets a value indicating whether CLI organization parameter.
    /// </summary>
    string Organization { get; set; }


    /// <summary>
    /// Gets or sets a value indicating whether CLI ignore unknown CA parameter.
    /// </summary>
    bool IgnoreUnknownCA { get; set; }

    /// <summary>
    /// Gets a value indicating whether is Oss scan enabled.
    /// </summary>
    bool OssEnabled { get; set; }
    bool IacEnabled { get; set; }

    /// <summary>
    /// Gets a value indicating whether is Oss scan enabled.
    /// </summary>
    bool SnykCodeSecurityEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the CLI should be automatically updated.
    /// </summary>
    bool BinariesAutoUpdate { get; set; }

    /// <summary>
    /// Gets or sets the value of the CLI custom path. If empty, the default path from AppData would be used.
    /// </summary>
    string CliCustomPath { get; set; }
    string CliReleaseChannel { get; set; }
    string CliBaseDownloadURL { get; set; }
    ISet<string> TrustedFolders { get; set; }

    bool EnableDeltaFindings { get; set; }
    List<FolderConfig> FolderConfigs { get; set; }
    string CurrentCliVersion { get; set; }
    bool AnalyticsPluginInstalledSent { get; set; }

    // Severity filters
    bool FilterCritical { get; set; }
    bool FilterHigh { get; set; }
    bool FilterMedium { get; set; }
    bool FilterLow { get; set; }

    string AdditionalEnv { get; set; }
    int? RiskScoreThreshold { get; set; }
}