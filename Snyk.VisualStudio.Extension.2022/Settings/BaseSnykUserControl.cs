// ABOUTME: Base class for Snyk user control settings pages that automatically syncs with HTML settings
// ABOUTME: Eliminates duplicate event handling code across all UserControl classes and supports both sync and async loading patterns

using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Base class for all Snyk settings user controls.
    /// Handles common functionality like settings synchronization with the HTML settings window.
    /// Supports both synchronous and asynchronous loading patterns.
    /// </summary>
    public class BaseSnykUserControl : UserControl
    {
        protected readonly ISnykServiceProvider serviceProvider;
        internal IPersistableOptions OptionsMemento { get; set; }

        /// <summary>
        /// Parameterless constructor for Visual Studio Designer support.
        /// Do not use this constructor in production code.
        /// </summary>
        public BaseSnykUserControl()
        {
        }

        public BaseSnykUserControl(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            // Load initial settings
            OptionsMemento = serviceProvider.SnykOptionsManager.Load();

            // Subscribe to settings changes to sync with HTML window
            serviceProvider.Options.SettingsChanged += OnSettingsChanged;
        }

        /// <summary>
        /// Called when settings change (e.g., from HTML settings window).
        /// Reloads settings from disk and updates the UI.
        /// Calls the async version to support both sync and async derived classes.
        /// </summary>
        private void OnSettingsChanged(object sender, SnykSettingsChangedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Reload settings from disk to get latest values
                OptionsMemento = serviceProvider.SnykOptionsManager.Load();

                // Update UI to reflect new settings (async version for derived classes)
                await UpdateViewFromOptionsAsync();
            }).FireAndForget();
        }

        /// <summary>
        /// Updates the control's UI elements to reflect current option values (synchronous version).
        /// Override this for simple controls that load from in-memory OptionsMemento.
        /// </summary>
        protected virtual void UpdateViewFromOptions()
        {
            // Default implementation does nothing - derived classes override as needed
        }

        /// <summary>
        /// Updates the control's UI elements to reflect current option values (asynchronous version).
        /// Override this for complex controls that need async I/O operations.
        /// Default implementation calls the synchronous version for backward compatibility.
        /// </summary>
        protected virtual Task UpdateViewFromOptionsAsync()
        {
            // Default: call synchronous version for backward compatibility
            // We're already on the UI thread after SwitchToMainThreadAsync in OnSettingsChanged
            UpdateViewFromOptions();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup when control is disposed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && serviceProvider?.Options != null)
            {
                serviceProvider.Options.SettingsChanged -= OnSettingsChanged;
            }

            base.Dispose(disposing);
        }
    }
}
