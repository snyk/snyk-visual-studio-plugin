// ABOUTME: WPF modal window for HTML-based settings interface
// ABOUTME: Uses WPF WebBrowser to display Language Server settings HTML

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.UI.Html;
using Snyk.VisualStudio.Extension.UI.Toolwindow;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// WPF modal window for HTML-based settings configuration.
    /// Loads settings HTML from Language Server and provides IDE bridge functions for save/login/logout.
    /// </summary>
    public partial class HtmlSettingsWindow : DialogWindow
    {
        protected static readonly ILogger Logger = LogManager.ForContext<HtmlSettingsWindow>();
        private static HtmlSettingsWindow instance;

        public static HtmlSettingsWindow Instance => instance;

        private readonly ISnykServiceProvider serviceProvider;
        private HtmlSettingsScriptingBridge scriptingBridge;
        protected WebBrowserHostUIHandler wbHandler;

        public static readonly DependencyProperty IsDirtyProperty =
            DependencyProperty.Register(
                "IsDirty",
                typeof(bool),
                typeof(HtmlSettingsWindow),
                new PropertyMetadata(false));

        public bool IsDirty
        {
            get => (bool)GetValue(IsDirtyProperty);
            set => SetValue(IsDirtyProperty, value);
        }

        public HtmlSettingsWindow(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Set as singleton instance
            instance = this;

            InitializeComponent();

            // Initialize WebBrowser handler
            wbHandler = new WebBrowserHostUIHandler(SettingsBrowser)
            {
                IsWebBrowserContextMenuEnabled = false,
                ScriptErrorsSuppressed = true,
            };
            // Disable DPI awareness for this window, since we haven't been able to find a way to support it properly and we are not scaling.
            // If we do not disable DPI awareness (without fixing the DPI scaling issue), then dropdown menus will be rendered incorrectly.
            wbHandler.SetDpiAwareFlag(false);

            wbHandler.LoadCompleted += SettingsBrowser_OnLoadCompleted;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadHtmlSettingsAsync();
        }

        private async Task LoadHtmlSettingsAsync()
        {
            try
            {
                LoadingStatusLabel.Text = "Loading settings...";

                // Set up scripting bridge for IDE integration (before loading HTML)
                SetupScriptingBridge();

                // Load HTML from Language Server or fallback
                var html = await GetHtmlContentAsync();

                if (string.IsNullOrEmpty(html))
                {
                    LoadingStatusLabel.Text = "Failed to load settings HTML";
                    return;
                }

                // Force visual refresh before navigation (required for DPI handling)
                SettingsBrowser.InvalidateVisual();
                SettingsBrowser.UpdateLayout();

                html = HtmlResourceLoader.ApplyTheme(html);
                SettingsBrowser.NavigateToString(html);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load HTML settings");
                LoadingStatusLabel.Text = $"Error loading settings: {ex.Message}";
            }
        }

        /// <summary>
        /// Sets up the scripting bridge for IDE integration.
        /// Called before loading HTML to ensure window.external is available.
        /// </summary>
        protected virtual void SetupScriptingBridge()
        {
            // Create scripting bridge with callbacks
            // Note: JavaScript -> ObjectForScripting calls are COM-marshaled to UI thread (STA)
            scriptingBridge = new HtmlSettingsScriptingBridge(
                serviceProvider,
                onModified: () => IsDirty = true,
                onReset: () => IsDirty = false);

            // Set the scripting bridge as the ObjectForScripting
            SettingsBrowser.ObjectForScripting = scriptingBridge;
        }

        private void SettingsBrowser_OnLoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                // Inject IDE bridge functions into window object
                InjectIdeBridgeFunctions();

                // Hide loading label
                LoadingStatusLabel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading settings content");
                LoadingStatusLabel.Text = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Loads HTML from Language Server with retries, falls back to embedded HTML if unavailable.
        /// </summary>
        protected virtual async Task<string> GetHtmlContentAsync()
        {
            // Check if Language Server is ready before attempting to get HTML
            if (!LanguageClientHelper.IsLanguageServerReady())
            {
                Logger.Warning("Language Server not ready, using fallback HTML");
                return HtmlResourceLoader.LoadFallbackHtml(serviceProvider.Options);
            }

            try
            {
                var lsHtml = await serviceProvider.LanguageClientManager.GetConfigHtmlAsync(
                    System.Threading.CancellationToken.None);
                if (!string.IsNullOrEmpty(lsHtml))
                {
                    Logger.Information("Successfully loaded settings HTML from Language Server");
                    return lsHtml;
                }
                Logger.Warning("Language Server returned empty HTML");
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to get HTML from Language Server");
            }

            // Fall back to embedded HTML
            Logger.Warning("Falling back to embedded HTML");
            return HtmlResourceLoader.LoadFallbackHtml(serviceProvider.Options);
        }

        /// <summary>
        /// Injects IDE bridge functions into the HTML window object for save/login/logout operations.
        /// </summary>
        protected virtual void InjectIdeBridgeFunctions()
        {
            try
            {
                dynamic doc = SettingsBrowser.Document;
                if (doc == null) return;

                // Inject bridge functions that LS HTML expects
                var script = @"
                    window.__saveIdeConfig__ = function(jsonString) {
                        window.external.__saveIdeConfig__(jsonString);
                    };
                    window.__ideLogin__ = function() {
                        window.external.__ideLogin__();
                    };
                    window.__ideLogout__ = function() {
                        window.external.__ideLogout__();
                    };
                    window.__onFormDirtyChange__ = function(isDirty) {
                        window.external.__onFormDirtyChange__(isDirty);
                    };
                    window.__ideSaveAttemptFinished__ = function(status) {
                        window.external.__ideSaveAttemptFinished__(status);
                    };
                ";

                var scriptElement = doc.CreateElement("script");
                scriptElement.SetAttribute("type", "text/javascript");
                scriptElement.InnerText = script;
                doc.GetElementsByTagName("head")[0].AppendChild(scriptElement);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error injecting IDE bridge functions");
            }
        }

        private async void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable buttons while saving
                OkButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                // Call the LS HTML's exposed function to collect and save config
                try
                {
                    SettingsBrowser.InvokeScript("getAndSaveIdeConfig");

                    // Wait for save to complete (with timeout)
                    var startTime = DateTime.Now;
                    while (!scriptingBridge.IsSaveComplete && (DateTime.Now - startTime).TotalSeconds < 5)
                    {
                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to invoke getAndSaveIdeConfig");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save settings");
                OkButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        public void UpdateAuthToken(string token)
        {
            try
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    if (SettingsBrowser?.Document != null)
                    {
                        InvokeSetAuthToken(token);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating auth token in HTML settings");
            }
        }

        private void InvokeSetAuthToken(string token)
        {
            try
            {
                // Inject script element to call setAuthToken function
                dynamic doc = SettingsBrowser.Document;
                if (doc == null)
                {
                    Logger.Warning("Document is null, cannot set auth token");
                    return;
                }

                var escapedToken = token.Replace("'", "\\'").Replace("\"", "\\\"");
                var script = $@"
                    (function() {{
                        if (typeof window.setAuthToken === 'function') {{
                            window.setAuthToken('{escapedToken}');
                        }} else {{
                            console.warn('window.setAuthToken is not available');
                        }}
                    }})();
                ";

                var scriptElement = doc.CreateElement("script");
                scriptElement.SetAttribute("type", "text/javascript");
                scriptElement.InnerText = script;
                doc.GetElementsByTagName("head")[0].AppendChild(scriptElement);

                Logger.Information("Invoked window.setAuthToken with token");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error invoking window.setAuthToken");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clear singleton instance
            if (instance == this)
            {
                instance = null;
            }

            base.OnClosed(e);
        }

    }
}
