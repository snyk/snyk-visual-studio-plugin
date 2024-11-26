using System.Collections.Generic;
using System.Threading.Tasks;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Settings;

public interface IUserStorageSettingsService
{
    void SaveSettings();
    bool BinariesAutoUpdate { get; set; }
    string CliCustomPath { get; set; }
    AuthenticationType AuthenticationMethod { get; set; }

    /// <summary>
    /// Gets or sets trusted folders list.
    /// </summary>
    ISet<string> TrustedFolders { get; set; }

    /// <summary>
    /// Get Auto Scan option
    /// </summary>
    /// <returns>bool.</returns>
    bool AutoScan { get; set; }

    /// <summary>
    /// Get Or Set Auth Token
    /// </summary>
    /// <returns>string.</returns>
    string Token { get; set; }

    string CliReleaseChannel { get; set; }
    string CliDownloadUrl { get; set; }
    bool IgnoreUnknownCa { get; set; }
    string Organization { get; set; }
    string CustomEndpoint { get; set; }
    bool SnykCodeSecurityEnabled { get; set; }
    bool SnykCodeQualityEnabled { get; set; }
    bool OssEnabled { get; set; }
    bool IacEnabled { get; set; }
    string CurrentCliVersion { get; set; }
    string DeviceId { get; set; }
    bool AnalyticsPluginInstalledSent { get; set; }
    bool OpenIssuesEnabled { get; set; }
    bool IgnoredIssuesEnabled { get; set; }
    List<FolderConfig> FolderConfigs { get; set; }
    bool EnableDeltaFindings { get; set; }

    /// <summary>
    /// Get is all projects enabled.
    /// </summary>
    /// <returns>Bool.</returns>
    Task<bool> GetIsAllProjectsEnabledAsync();

    /// <summary>
    /// Get CLI additional options string.
    /// </summary>
    /// <returns>string.</returns>
    Task<string> GetAdditionalOptionsAsync();

    /// <summary>
    /// Save additional options string.
    /// </summary>
    /// <param name="additionalOptions">CLI options string.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAdditionalOptionsAsync(string additionalOptions);

    /// <summary>
    /// Sace is all projects scan enabled.
    /// </summary>
    /// <param name="isAllProjectsEnabled">Bool param.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveIsAllProjectsScanEnabledAsync(bool isAllProjectsEnabled);
}