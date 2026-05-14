// ABOUTME: WPF modal window for HTML-based settings interface
// ABOUTME: Hosts the Language Server settings HTML in a WebView2 control

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.UI.Html;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// WPF modal window for HTML-based settings configuration.
    /// Loads settings HTML from the Language Server and provides IDE bridge functions
    /// for save/login/logout, routed through <see cref="WebView2Host"/>'s
    /// <c>window.external.*</c> surface.
    /// </summary>
    public partial class HtmlSettingsWindow : DialogWindow
    {
        protected static readonly ILogger Logger = LogManager.ForContext<HtmlSettingsWindow>();
        private static volatile HtmlSettingsWindow instance;

        public static HtmlSettingsWindow Instance => instance;

        /// <summary>
        /// Aliases <c>window.__saveIdeConfig__</c>, <c>window.__onFormDirtyChange__</c>, and
        /// <c>window.__ideSaveAttemptFinished__</c> to their <c>window.external.*</c> equivalents
        /// (which the WebView2 polyfill in turn routes via <c>chrome.webview.postMessage</c>).
        /// The LS-authored settings page calls the <c>window.X</c> form directly.
        /// </summary>
        private const string SettingsBridgeAliasesScript =
            @"window.__saveIdeConfig__ = function(jsonString) { window.external.__saveIdeConfig__(jsonString); };
              window.__onFormDirtyChange__ = function(isDirty) { window.external.__onFormDirtyChange__(isDirty); };
              window.__ideSaveAttemptFinished__ = function(status) { window.external.__ideSaveAttemptFinished__(status); };";

        private readonly ISnykServiceProvider serviceProvider;
        protected HtmlSettingsScriptingBridge scriptingBridge;
        protected WebView2Host host;

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

            // Create scripting bridge with callbacks
            scriptingBridge = new HtmlSettingsScriptingBridge(
                serviceProvider,
                onModified: () => IsDirty = true,
                onReset: () => IsDirty = false,
                onCommandResult: InvokeCommandCallback);

            var dispatcher = new WebView2MessageDispatcher()
                .Register("__saveIdeConfig__", args =>
                    scriptingBridge.__saveIdeConfig__(args[0].Value<string>()))
                .Register("__onFormDirtyChange__", args =>
                    scriptingBridge.__onFormDirtyChange__(args[0].Value<bool>()))
                .Register("__ideSaveAttemptFinished__", args =>
                    scriptingBridge.__ideSaveAttemptFinished__(args[0].Value<string>()))
                .Register("__ideExecuteCommand__", args =>
                    scriptingBridge.__ideExecuteCommand__(
                        args[0].Value<string>(),
                        args[1].Value<string>(),
                        args[2].Value<string>()));

            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Snyk", "WebView2", "settings");

            host = new WebView2Host(
                SettingsBrowser,
                dispatcher,
                userDataFolder,
                additionalInitScripts: new[]
                {
                    ExecuteCommandBridge.BuildClientScript(),
                    SettingsBridgeAliasesScript,
                });

            SettingsBrowser.NavigationCompleted += SettingsBrowser_OnNavigationCompleted;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await host.InitializeAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "WebView2 initialization failed");
                LoadingStatusLabel.Text = $"Failed to initialize browser: {ex.Message}";
                return;
            }

            await LoadHtmlSettingsAsync();
        }

        private async Task LoadHtmlSettingsAsync()
        {
            try
            {
                LoadingStatusLabel.Text = "Loading settings...";

                // Load HTML from Language Server or fallback
                var html = await GetHtmlContentAsync();
                if (string.IsNullOrEmpty(html))
                {
                    LoadingStatusLabel.Text = "Failed to load settings HTML";
                    return;
                }

                html = HtmlResourceLoader.ApplyTheme(html);
                await host.NavigateAsync(html);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load HTML settings");
                LoadingStatusLabel.Text = $"Error loading settings: {ex.Message}";
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
                    SnykVSPackage.Instance.DisposalToken);
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

        private void SettingsBrowser_OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Hide loading label
            LoadingStatusLabel.Visibility = Visibility.Collapsed;
        }

        private async void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                OkButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                try
                {
                    scriptingBridge.BeginSave();
                    await host.ExecuteScriptAsync("getAndSaveIdeConfig()");

                    var saveTask = scriptingBridge.SaveCompletion;
                    var winner = await Task.WhenAny(saveTask, Task.Delay(TimeSpan.FromSeconds(5)));
                    if (winner != saveTask)
                    {
                        Logger.Warning("Save did not complete within timeout; closing anyway");
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

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        public void UpdateAuthToken(string token, string apiUrl = null)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    await host.ExecuteScriptAsync(
                        ExecuteCommandBridge.BuildSetAuthTokenScript(token, apiUrl));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error invoking window.setAuthToken");
                }
            }).FireAndForget();
        }

        private void InvokeCommandCallback(string callbackId, string resultJson)
        {
            if (!ExecuteCommandBridge.IsValidCallbackId(callbackId))
            {
                Logger.Warning("Rejected callbackId with unexpected format: {CallbackId}", callbackId);
                return;
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    await host.ExecuteScriptAsync(
                        ExecuteCommandBridge.BuildCommandCallbackScript(callbackId, resultJson));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error invoking command callback {CallbackId}", callbackId);
                }
            }).FireAndForget();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (instance == this)
            {
                instance = null;
            }

            host?.Dispose();

            base.OnClosed(e);
        }
    }
}
