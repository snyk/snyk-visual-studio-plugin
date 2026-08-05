using System;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Theme;
using Snyk.VisualStudio.Extension.UI.Toolwindow;

namespace Snyk.VisualStudio.Extension.Service
{
    /// <summary>
    /// The Visual Studio bound half of <see cref="ISnykServiceProvider"/>. Everything here needs
    /// the VS SDK, so this part is excluded from the cross-platform build (see
    /// docs/cross-platform-testing.md).
    /// </summary>
    public partial interface ISnykServiceProvider
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
        /// Gets Visual Studio Settiings Manager instance.
        /// </summary>
        SettingsManager SettingsManager { get; }

        /// <summary>
        /// Gets Theme service instance.
        /// </summary>
        SnykVsThemeService VsThemeService { get; }

        /// <summary>
        /// Gets the tool window seam (implemented by <see cref="SnykToolWindowControl"/>).
        /// </summary>
        ISnykToolWindow ToolWindow { get; }

        /// <summary>
        /// Get Visual Studio service (async).
        /// </summary>
        /// <param name="serviceType">Service type.</param>
        /// <returns>VS service instance.</returns>
        Task<object> GetServiceAsync(Type serviceType);
    }
}
