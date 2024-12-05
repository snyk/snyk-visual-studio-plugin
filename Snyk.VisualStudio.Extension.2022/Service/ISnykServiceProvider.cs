using System;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Settings;
using Snyk.VisualStudio.Extension.Theme;
using Snyk.VisualStudio.Extension.UI.Toolwindow;

namespace Snyk.VisualStudio.Extension.Service
{
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

        IWorkspaceTrustService WorkspaceTrustService { get; }

        /// <summary>
        /// Gets Tasks service instance.
        /// </summary>
        ISnykTasksService TasksService { get; }

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
        /// Gets user storage settings service instance.
        /// </summary>
        IUserStorageSettingsService UserStorageSettingsService { get; }

        /// <summary>
        /// Gets <see cref="SnykToolWindowControl"/> instance.
        /// </summary>
        SnykToolWindowControl ToolWindow { get; }

        /// <summary>
        /// Get Visual Studio service (async).
        /// </summary>
        /// <param name="serviceType">Service type.</param>
        /// <returns>VS service instance.</returns>
        Task<object> GetServiceAsync(Type serviceType);
        
        /// <summary>
        /// Get Feature Flag Service
        /// </summary>
        IFeatureFlagService FeatureFlagService { get; }
        
        /// <summary>
        /// Get Language Client Manager
        /// </summary>
        ILanguageClientManager LanguageClientManager { get; set; }
    }
}
