using Snyk.Analytics;

namespace Snyk.VisualStudio.Extension.Shared.Service
{
    using System;
    using System.Threading.Tasks;
    using EnvDTE80;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Snyk.Code.Library.Service;
    using Snyk.Common;
    using Snyk.VisualStudio.Extension.Shared.CLI;
    using Snyk.VisualStudio.Extension.Shared.Settings;
    using Snyk.VisualStudio.Extension.Shared.SnykAnalytics;
    using Snyk.VisualStudio.Extension.Shared.Theme;
    using Snyk.VisualStudio.Extension.Shared.UI.Toolwindow;

    /// <summary>
    /// ServiceProvider interface for Snyk extension. Provide all needed services for this extension.
    /// </summary>
    public interface ISnykServiceProvider
    {
        /// <summary>
        /// Gets VisualStudio DTE object instance.
        /// </summary>
        DTE2 DTE { get; }

        /// <summary>
        /// Gets Snyk package instance.
        /// </summary>
        SnykVSPackage Package { get; }

        /// <summary>
        /// Gets IAsyncServiceProvider implementation.
        /// </summary>
        IAsyncServiceProvider AsyncServiceProvider { get; }

        /// <summary>
        /// Gets Solution service instance.
        /// </summary>
        ISolutionService SolutionService { get; }

        /// <summary>
        /// Gets Tasks service instance.
        /// </summary>
        SnykTasksService TasksService { get; }

        /// <summary>
        /// Gets <see cref="ISnykOptions"/> (Settings) implementation instance.
        /// </summary>
        ISnykOptions Options { get; }

        /// <summary>
        /// Gets Visual Studio Settiings Manager instance.
        /// </summary>
        SettingsManager SettingsManager { get; }

        /// <summary>
        /// Gets Theme service instance.
        /// </summary>
        SnykVsThemeService VsThemeService { get; }

        /// <summary>
        /// Gets SnykCodeService service instance.
        /// </summary>
        ISnykCodeService SnykCodeService { get; }

        /// <summary>
        /// Gets OssService service instance.
        /// </summary>
        IOssService OssService { get; }

        /// <summary>
        /// Gets Analytics service instance.
        /// </summary>
        ISnykAnalyticsService AnalyticsService { get; }

        /// <summary>
        /// Gets user storage settings service instance.
        /// </summary>
        SnykUserStorageSettingsService UserStorageSettingsService { get; }

        /// <summary>
        /// Gets <see cref="ISnykApiService"/> service instance.
        /// </summary>
        ISnykApiService ApiService { get; }

        /// <summary>
        /// Gets <see cref="SentryService"/> instance.
        /// </summary>
        /// <returns>Task.</returns>
        ISentryService SentryService { get; }

        /// <summary>
        /// Gets <see cref="SnykToolWindowControl"/> instance.
        /// </summary>
        SnykToolWindowControl ToolWindow { get; }

        /// <summary>
        /// Create new instance of <see cref="SnykCli"/>.
        /// </summary>
        /// <returns>SnykCli.</returns>
        ICli NewCli();

        /// <summary>
        /// Get User Snyk API token.
        /// </summary>
        /// <returns>User API token string.</returns>
        string GetApiToken();

        /// <summary>
        /// Get Visual Studio service (async).
        /// </summary>
        /// <param name="serviceType">Service type.</param>
        /// <returns>VS service instance.</returns>
        Task<object> GetServiceAsync(Type serviceType);
    }
}
