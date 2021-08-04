namespace Snyk.VisualStudio.Extension.Service
{
    using System;
    using System.Threading.Tasks;
    using EnvDTE;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Snyk.Code.Library.Service;
    using Snyk.VisualStudio.Extension.CLI;
    using Snyk.VisualStudio.Extension.Settings;
    using Snyk.VisualStudio.Extension.SnykAnalytics;
    using Snyk.VisualStudio.Extension.Theme;

    /// <summary>
    /// ServiceProvider interface for Snyk extension. Provide all needed services for this extension.
    /// </summary>
    public interface ISnykServiceProvider
    {
        /// <summary>
        /// Gets VisualStudio DTE object instance.
        /// </summary>
        DTE DTE { get; }

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
        SnykSolutionService SolutionService { get; }

        /// <summary>
        /// Gets Tasks service instance.
        /// </summary>
        SnykTasksService TasksService { get; }

        /// <summary>
        /// Gets <see cref="ISnykOptions"/> (Settings) implementation instance.
        /// </summary>
        ISnykOptions Options { get; }

        /// <summary>
        /// Gets Visual Studio logger instance.
        /// </summary>
        SnykActivityLogger ActivityLogger { get; }

        /// <summary>
        /// Gets Visual Studio Settiings Manager instance.
        /// </summary>
        SettingsManager SettingsManager { get; }

        /// <summary>
        /// Gets Theme service instance.
        /// </summary>
        SnykVsThemeService VsThemeService { get; }

        /// <summary>
        /// Gets Theme service instance.
        /// </summary>
        ISnykCodeService SnykCodeService { get; }

        /// <summary>
        /// Gets Analytics service instance.
        /// </summary>
        SnykAnalyticsService AnalyticsService { get; }

        /// <summary>
        /// Gets user storage settings service instance.
        /// </summary>
        SnykUserStorageSettingsService UserStorageSettingsService { get; }

        /// <summary>
        /// Create new instance of <see cref="SnykCli"/>.
        /// </summary>
        /// <returns>SnykCli.</returns>
        SnykCli NewCli();

        /// <summary>
        /// Show Snyk tool window panel.
        /// </summary>
        void ShowToolWindow();

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
