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

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// WPF modal window for HTML-based settings configuration.
    /// </summary>
    public partial class HtmlSettingsWindow : DialogWindow
    {
        private static readonly ILogger Logger = LogManager.ForContext<HtmlSettingsWindow>();
        private static HtmlSettingsWindow instance;

        public static HtmlSettingsWindow Instance => instance;

        private readonly ISnykOptions options;
        private readonly IJsonRpc languageServerRpc;
        private readonly ISnykOptionsManager optionsManager;
        private readonly ISnykServiceProvider serviceProvider;
        private ConfigScriptingBridge scriptingBridge;
        private UI.Toolwindow.WebBrowserHostUIHandler wbHandler;

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

        public HtmlSettingsWindow(
            ISnykOptions options,
            IJsonRpc languageServerRpc,
            ISnykOptionsManager optionsManager,
            ISnykServiceProvider serviceProvider)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.languageServerRpc = languageServerRpc;
            this.optionsManager = optionsManager ?? throw new ArgumentNullException(nameof(optionsManager));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Set as singleton instance
            instance = this;

            // Clean up any previous registry modifications
            CleanupBrowserFeatureControl();

            InitializeComponent();

            // Initialize WebBrowser handler exactly like HtmlDescriptionPanel
            wbHandler = new UI.Toolwindow.WebBrowserHostUIHandler(SettingsBrowser)
            {
                IsWebBrowserContextMenuEnabled = false,
                ScriptErrorsSuppressed = true,
            };

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
                DebugLabel.Text = "Loading settings...";

                // Create scripting bridge with callbacks
                scriptingBridge = new ConfigScriptingBridge(
                    options,
                    onModified: () =>
                    {
                        ThreadHelper.JoinableTaskFactory.Run(async () =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            IsDirty = true;
                        });
                    },
                    onReset: () =>
                    {
                        ThreadHelper.JoinableTaskFactory.Run(async () =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            IsDirty = false;
                        });
                    },
                    optionsManager: optionsManager,
                    serviceProvider: serviceProvider);

                // Load HTML from Language Server or fallback
                var html = await GetHtmlContentAsync();

                if (string.IsNullOrEmpty(html))
                {
                    DebugLabel.Text = "Failed to load settings HTML";
                    return;
                }

                // Set the scripting bridge as the ObjectForScripting
                SettingsBrowser.ObjectForScripting = scriptingBridge;

                // Force visual refresh before navigation (required for DPI handling)
                SettingsBrowser.InvalidateVisual();
                SettingsBrowser.UpdateLayout();

                // TEMPORARY TEST: Use simple HTML to verify DPI handling
                var testHtml = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta http-equiv='X-UA-Compatible' content='IE=edge' />
                        <meta name='viewport' content='width=device-width, initial-scale=1.0' />
                        <style>
                            body {
                                font-family: 'Segoe UI', Arial, sans-serif;
                                margin: 20px;
                                padding: 0;
                            }
                            h1 { font-size: 48px; }
                            p { font-size: 28px; line-height: 1.5; }
                            .test-box {
                                border: 2px solid #0078d4;
                                padding: 15px;
                                margin: 10px 0;
                                background: #f3f3f3;
                            }
                            input, select, button {
                                font-size: 14px;
                                padding: 8px;
                                margin: 5px;
                            }
                        </style>
                    </head>
                    <body>
                        <h1>DPI Test - Settings Window</h1>
                        <div class='test-box'>
                            <p>This is 14px text. If font sizes look correct, DPI handling is working.</p>
                            <p>Testing various elements:</p>
                            <input type='text' value='Text input' />
                            <select><option>Dropdown</option></select>
                            <button>Button</button>
                        </div>
                        <p>Actual content HTML length: " + html.Length + @" characters</p>
                    </body>
                    </html>
                ";

                // Navigate to test HTML instead of actual content
                //SettingsBrowser.NavigateToString(testHtml);

                // TODO: Remove test HTML and uncomment these lines once DPI is verified:
                html = HtmlResourceLoader.ApplyTheme(html);
                SettingsBrowser.NavigateToString(html);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load HTML settings");
                DebugLabel.Text = $"Error loading settings: {ex.Message}";
            }
        }

        private void SettingsBrowser_OnLoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                // Inject IDE bridge functions into window object
                InjectIdeBridgeFunctions();

                // Hide loading label
                DebugLabel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading settings content");
                DebugLabel.Text = $"Error: {ex.Message}";
            }
        }


        private async Task<string> GetHtmlContentAsync()
        {
            // Try to get HTML from Language Server (with retries)
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var lsHtml = await LanguageServerCommands.GetConfigHtmlAsync(
                        languageServerRpc,
                        System.Threading.CancellationToken.None);
                    if (!string.IsNullOrEmpty(lsHtml))
                    {
                        Logger.Information("Successfully loaded settings HTML from Language Server");
                        return lsHtml;
                    }
                    Logger.Warning("Language Server returned empty HTML on attempt {Attempt}", i + 1);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to get HTML from Language Server on attempt {Attempt} of 3", i + 1);
                }

                if (i < 2)
                    await Task.Delay(1000);
            }

            // Fall back to embedded HTML
            Logger.Warning("Falling back to embedded HTML after 3 failed Language Server attempts");
            return HtmlResourceLoader.LoadFallbackHtml(options);
        }

        private void InjectIdeBridgeFunctions()
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

        /// <summary>
        /// Clean up any previous IE11 feature control registry modifications.
        /// Removes registry entries that were previously set by SetBrowserFeatureControl.
        /// </summary>
        private void CleanupBrowserFeatureControl()
        {
            try
            {
                var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

                // List of features that were previously set
                var features = new[]
                {
                    "FEATURE_BROWSER_EMULATION",
                    "FEATURE_DISABLE_NAVIGATION_SOUNDS",
                    "FEATURE_SCRIPTURL_MITIGATION",
                    "FEATURE_96DPI_PIXEL",
                    "FEATURE_NINPUT_LEGACYMODE"
                };

                foreach (var feature in features)
                {
                    CleanupBrowserFeatureControlKey(feature, appName);
                }

                Logger.Information("Successfully cleaned up browser feature control registry entries");
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error cleaning up browser feature control");
            }
        }

        private void CleanupBrowserFeatureControlKey(string feature, string appName)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    $@"Software\Microsoft\Internet Explorer\Main\FeatureControl\{feature}",
                    writable: true))
                {
                    if (key != null)
                    {
                        // Delete the value if it exists
                        var value = key.GetValue(appName);
                        if (value != null)
                        {
                            key.DeleteValue(appName, throwOnMissingValue: false);
                            Logger.Information("Removed registry value for {Feature}: {AppName}", feature, appName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error cleaning up browser feature control key: {Feature}", feature);
            }
        }

    }
}
