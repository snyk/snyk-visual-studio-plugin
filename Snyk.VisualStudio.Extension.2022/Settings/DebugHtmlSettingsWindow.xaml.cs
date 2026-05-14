// ABOUTME: Debug-only version of HtmlSettingsWindow for testing with local files
// ABOUTME: Loads config_output.html from disk for rapid CSS/layout iteration without Language Server

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Service;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Debug version of HtmlSettingsWindow that loads HTML from a local file instead of
    /// the Language Server. Inherits all UI/XAML from HtmlSettingsWindow, only overrides
    /// data loading behaviour. Use this for rapid iteration on CSS and layout changes
    /// without waiting for LS initialization. The debug HTML supplies its own mock
    /// <c>window.__saveIdeConfig__</c>/etc. implementations, which override our bridge
    /// bindings at page-load time, so the real scripting bridge — though wired — never
    /// receives messages.
    /// </summary>
    public class DebugHtmlSettingsWindow : HtmlSettingsWindow
    {
        /// <summary>
        /// Expose Chromium DevTools (F12) and the default right-click context menu so
        /// developers can inspect JS errors and the DOM while iterating on the LS HTML.
        /// </summary>
        protected override bool DeveloperToolsEnabled => true;

        /// <summary>
        /// Path to the local HTML file to load for testing.
        /// Generate this file by running in snyk-ls: go run scripts/config-dialog/main.go > config_output.html
        /// </summary>
        private const string DEBUG_HTML_FILE_PATH = @"C:\Mac\Home\Documents\Snyk\snyk-ls\config_output.html";

        /// <summary>
        /// Set to true to automatically open this debug window when the Visual Studio extension loads.
        /// This allows rapid testing of CSS/HTML changes without manually opening settings.
        /// </summary>
        public static bool AutoOpenOnStartup = false;

        /// <summary>
        /// Opens the debug window on extension startup if AutoOpenOnStartup is enabled.
        /// Called from SnykVSPackage.InitializeAsync().
        /// </summary>
        /// <param name="serviceProvider">The Snyk service provider for accessing dependencies</param>
        public static async Task OpenDebugWindowIfEnabledAsync(ISnykServiceProvider serviceProvider)
        {
            if (!AutoOpenOnStartup)
            {
                return;
            }

            try
            {
                // Ensure we're on the UI thread since we're creating a WPF window
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                Logger.Information("DEBUG MODE: Opening DebugHtmlSettingsWindow for local file testing");

                var debugWindow = new DebugHtmlSettingsWindow(serviceProvider);
                debugWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "DEBUG MODE: Failed to open debug window on startup");
            }
        }

        /// <summary>
        /// Initializes a new debug HTML settings window.
        /// Passes serviceProvider to base HtmlSettingsWindow constructor.
        /// </summary>
        public DebugHtmlSettingsWindow(ISnykServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            // Add debug indicator to title after base initialization
            this.Loaded += (sender, args) =>
            {
                this.Title += " [DEBUG MODE - Local HTML]";
            };
        }

        /// <summary>
        /// Loads HTML from local file system for rapid CSS/layout testing.
        /// This bypasses the Language Server entirely.
        /// </summary>
        protected override Task<string> GetHtmlContentAsync()
        {
            try
            {
                if (!System.IO.File.Exists(DEBUG_HTML_FILE_PATH))
                {
                    Logger.Error("DEBUG MODE: Local HTML file not found at {Path}", DEBUG_HTML_FILE_PATH);
                    return Task.FromResult(
                        $"<html><body style='font-family: sans-serif; padding: 20px;'>" +
                        $"<h1>ERROR: Debug file not found</h1>" +
                        $"<p>DEBUG_HTML_FILE_PATH is set to: <code>{DEBUG_HTML_FILE_PATH}</code></p>" +
                        $"<p>Generate it in snyk-ls by running:</p>" +
                        $"<pre>cd /path/to/snyk-ls\ngo run scripts/config-dialog/main.go > config_output.html</pre>" +
                        $"<p>Then update the DEBUG_HTML_FILE_PATH in DebugHtmlSettingsWindow.xaml.cs to the correct file path.</p>" +
                        $"</body></html>");
                }

                var html = System.IO.File.ReadAllText(DEBUG_HTML_FILE_PATH);
                Logger.Information("DEBUG MODE: Loaded HTML from local file: {Path} ({Length} chars)",
                    DEBUG_HTML_FILE_PATH, html.Length);

                return Task.FromResult(html);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "DEBUG MODE: Failed to load local HTML file");
                return Task.FromResult(
                    $"<html><body style='font-family: sans-serif; padding: 20px;'>" +
                    $"<h1>ERROR loading debug file</h1>" +
                    $"<p>{ex.Message}</p>" +
                    $"</body></html>");
            }
        }
    }
}

