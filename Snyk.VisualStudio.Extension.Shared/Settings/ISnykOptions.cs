namespace Snyk.VisualStudio.Extension.Shared.Settings
{
    using System;

    /// <summary>
    /// Interface for Snyk Options/Settings in Visual Studio.
    /// </summary>
    public interface ISnykOptions
    {
        /// <summary>
        /// Settings changed event.
        /// </summary>
        event EventHandler<SnykSettingsChangedEventArgs> SettingsChanged;

        /// <summary>
        /// Gets or sets a value indicating whether Snyk user API token.
        /// </summary>
        string ApiToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether CLI custom endpoint parameter.
        /// </summary>
        string CustomEndpoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether CLI organization parameter.
        /// </summary>
        string Organization { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether CLI ignore unknown CA parameter.
        /// </summary>
        bool IgnoreUnknownCA { get; set; }

        /// <summary>
        /// Gets a value indicating whether CLI additional parameters for current solution/project.
        /// </summary>
        string AdditionalOptions { get; }

        /// <summary>
        /// Gets a value indicating whether is CLI --all-projects parameter added by default. By default it's enabled.
        /// </summary>
        bool IsScanAllProjects { get; }

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
        /// Call CLI auth for user authentication at Snyk and get user api token.
        /// </summary>
        /// <param name="successCallbackAction">Callback for success authentication case.</param>
        /// <param name="errorCallbackAction">Callback for error on authentication case.</param>
        void Authenticate(Action<string> successCallbackAction, Action<string> errorCallbackAction);

        /// <summary>
        /// Force Visual Studio to load Settings from storage.
        /// </summary>
        void LoadSettingsFromStorage();
    }
}
