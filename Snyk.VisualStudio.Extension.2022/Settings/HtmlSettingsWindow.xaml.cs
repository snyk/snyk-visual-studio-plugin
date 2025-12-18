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
        private readonly ISnykOptions options;
        private readonly IJsonRpc languageServerRpc;
        private readonly ISnykOptionsManager optionsManager;
        private readonly ISnykServiceProvider serviceProvider;
        private ConfigScriptingBridge scriptingBridge;

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

            // Set IE11 feature control for modern rendering
            SetBrowserFeatureControl();

            InitializeComponent();
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

                // Apply theme
                html = HtmlResourceLoader.ApplyTheme(html);

                // Set the scripting bridge as the ObjectForScripting
                SettingsBrowser.ObjectForScripting = scriptingBridge;

                // Navigate to the HTML
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
                // Ensure scripting bridge is set
                SettingsBrowser.ObjectForScripting = scriptingBridge;

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

        /// <summary>
        /// Set IE11 feature control for modern rendering mode.
        /// This ensures the WebBrowser control uses IE11 mode instead of IE7 compatibility mode.
        /// </summary>
        private void SetBrowserFeatureControl()
        {
            try
            {
                var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", appName, 11001); // IE11 mode
                SetBrowserFeatureControlKey("FEATURE_DISABLE_NAVIGATION_SOUNDS", appName, 1);
                SetBrowserFeatureControlKey("FEATURE_SCRIPTURL_MITIGATION", appName, 1);
                SetBrowserFeatureControlKey("FEATURE_96DPI_PIXEL", appName, 1); // Force 96 DPI pixel scaling
                SetBrowserFeatureControlKey("FEATURE_NINPUT_LEGACYMODE", appName, 0); // Disable legacy input mode
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error setting browser feature control");
            }
        }

        private void SetBrowserFeatureControlKey(string feature, string appName, int value)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    $@"Software\Microsoft\Internet Explorer\Main\FeatureControl\{feature}",
                    Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    key?.SetValue(appName, value, Microsoft.Win32.RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error setting browser feature control key: {feature}");
            }
        }
    }
}
