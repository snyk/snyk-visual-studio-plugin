using System;
using System.Threading.Tasks;
using Snyk.Common.Authentication;
using Snyk.Common.Service;

namespace Snyk.Common.Settings
{
    /// <summary>
    /// Interface for Snyk Options/Settings in Visual Studio.
    /// </summary>
    public interface ISnykOptions
    {
        string Application { get; set; }
        string ApplicationVersion { get; set; }
        string IntegrationName { get; }
        string IntegrationVersion { get; }
        string IntegrationEnvironment { get; set; }
        string IntegrationEnvironmentVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Snyk user API token.
        /// </summary>
        AuthenticationToken ApiToken { get; }

        /// <summary>
        /// Gets Value of Authentication Token Type.
        /// </summary>
        AuthenticationType AuthenticationMethod { get; }

        SnykUser SnykUser { get; set; }

        bool IsFedramp();

        bool IsAnalyticsPermitted();

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
        bool OssEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether is Oss scan enabled.
        /// </summary>
        bool SnykCodeSecurityEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether is Oss scan enabled.
        /// </summary>
        bool SnykCodeQualityEnabled { get; }

        /// <summary>
        /// Gets or sets a value indicating whether Sentry anonymous user id.
        /// </summary>
        string AnonymousId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Analytics enabled or disabled. By default it's enabled.
        /// </summary>
        bool UsageAnalyticsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the CLI should be automatically updated.
        /// </summary>
        bool BinariesAutoUpdate { get; set; }

        /// <summary>
        /// Gets or sets the value of the CLI custom path. If empty, the default path from AppData would be used.
        /// </summary>
        string CliCustomPath { get; set; }

        /// <summary>
        /// Settings changed event.
        /// </summary>
        event EventHandler<SnykSettingsChangedEventArgs> SettingsChanged;

        void SetApiToken(string apiToken);

        /// <summary>
        /// Gets a value indicating whether additional options.
        /// Get this data using <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        Task<string> GetAdditionalOptionsAsync();

        /// <summary>
        /// Gets a value indicating whether is scan all projects enabled via <see cref="SnykUserStorageSettingsService"/>.
        /// Get this data using <see cref="SnykUserStorageSettingsService"/>.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        Task<bool> IsScanAllProjectsAsync();

        /// <summary>
        /// Attempts to pull the token from the CLI config storage, and validates the token.
        /// If the token is invalid, attempts to run the authentication command.
        /// </summary>
        /// <returns>Returns true if authenticated successfully, or if a valid token was loaded from storage.</returns>
        bool Authenticate();

        /// <summary>
        /// Force Visual Studio to load Settings from storage.
        /// </summary>
        void LoadSettingsFromStorage();

        SastSettings SastSettings { get; set; }
    }
}