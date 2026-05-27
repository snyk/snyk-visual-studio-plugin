// ABOUTME: WebView2-backed settings UserControl. Hosts the LS-rendered HTML config form.
// ABOUTME: Hosted by HtmlSettingsDialogPage as the Tools->Options "Snyk" page.

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using Serilog;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Service;
using Snyk.VisualStudio.Extension.UI.Html;
using Snyk.VisualStudio.Extension.UI.Toolwindow;

namespace Snyk.VisualStudio.Extension.Settings
{
    /// <summary>
    /// Hosts the Language Server settings HTML in a WebView2 control, wires the JS↔C# bridge,
    /// and exposes the save / auth-push / dirty-state hooks the embedding Tools→Options
    /// <see cref="HtmlSettingsDialogPage"/> needs.
    /// </summary>
    public partial class HtmlSettingsControl : UserControl, IDisposable
    {
        protected static readonly ILogger Logger = LogManager.ForContext<HtmlSettingsControl>();

        // Live control instance, updated on Loaded/Unloaded. Used by SnykLanguageClientCustomTarget
        // to push LS-driven auth tokens into the currently-visible settings page after an OAuth
        // round-trip. Volatile because writers (WPF Loaded/Unloaded on UI thread) and readers
        // (LS callback on a background thread) cross threads.
        private static volatile HtmlSettingsControl instance;

        /// <summary>
        /// The currently-loaded settings control, or null if no settings UI is visible.
        /// Tracks the Tools→Options <see cref="HtmlSettingsDialogPage"/>'s control while
        /// the page is in the visual tree.
        /// </summary>
        public static HtmlSettingsControl Instance => instance;

        private readonly ISnykServiceProvider serviceProvider;
        protected readonly HtmlSettingsScriptingBridge scriptingBridge;
        protected readonly WebView2Host host;
        private bool _disposed;

        public static readonly DependencyProperty IsDirtyProperty =
            DependencyProperty.Register(
                nameof(IsDirty),
                typeof(bool),
                typeof(HtmlSettingsControl),
                new PropertyMetadata(false));

        public bool IsDirty
        {
            get => (bool)GetValue(IsDirtyProperty);
            set => SetValue(IsDirtyProperty, value);
        }

        /// <summary>
        /// Whether to expose Chromium DevTools (F12) on the hosted WebView2. Gated on the
        /// DEBUG compile constant so Release builds never ship DevTools, while local
        /// developer builds get inspect-element and the JS console for free.
        /// </summary>
        protected virtual bool DeveloperToolsEnabled =>
#if DEBUG
            true;
#else
            false;
#endif

        public HtmlSettingsControl(ISnykServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            InitializeComponent();

            scriptingBridge = new HtmlSettingsScriptingBridge(
                serviceProvider,
                onModified: () => IsDirty = true,
                onReset: () => IsDirty = false,
                onCommandResult: InvokeCommandCallback);

            // BaseHtmlProvider's injected link-click interceptor calls window.OpenLink for every
            // anchor (Privacy Policy, Documentation, etc.) — must be wired through the dispatcher
            // or the clicks silently no-op.
            var linkOpener = new SnykScriptManager(serviceProvider);

            // Intentionally not wiring __IS_IDE_AUTOSAVE_ENABLED__: VS and IntelliJ use OK-button
            // apply, only VS Code autosaves. The LS HTML / fallback HTML default of "absent flag
            // → don't autosave" gives us the right VS dialog-commit behaviour.
            var dispatcher = new WebView2MessageDispatcher()
                .Register("__saveIdeConfig__", 1, args =>
                    scriptingBridge.__saveIdeConfig__(args[0].Value<string>()))
                .Register("__onFormDirtyChange__", 1, args =>
                    scriptingBridge.__onFormDirtyChange__(args[0].Value<bool>()))
                .Register("__ideSaveAttemptFinished__", 1, args =>
                    scriptingBridge.__ideSaveAttemptFinished__(args[0].Value<string>()))
                .Register("__ideExecuteCommand__", 3, args =>
                    scriptingBridge.__ideExecuteCommand__(
                        args[0].Value<string>(),
                        args[1].Value<string>(),
                        args[2].Value<string>()))
                .Register("OpenLink", 1, args => linkOpener.OpenLink(args[0].Value<string>()));

            var userDataFolder = WebView2Host.BuildUserDataFolder("settings");

            host = new WebView2Host(
                SettingsBrowser,
                dispatcher,
                userDataFolder,
                additionalInitScripts: new[]
                {
                    ExecuteCommandBridge.BuildClientScript(),
                },
                enableDeveloperTools: DeveloperToolsEnabled);

            SettingsBrowser.NavigationCompleted += SettingsBrowser_OnNavigationCompleted;
            Unloaded += Control_Unloaded;
        }

        private async void Control_Loaded(object sender, RoutedEventArgs e)
        {
            instance = this;

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

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            // Clear the singleton if we're the currently-tracked control — guards against
            // older instances racing the latest Loaded write.
            if (instance == this)
            {
                instance = null;
            }
        }

        private async Task LoadHtmlSettingsAsync()
        {
            try
            {
                LoadingStatusLabel.Text = "Loading settings...";

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
        /// Loads HTML from Language Server, falls back to embedded HTML if unavailable.
        /// </summary>
        protected virtual async Task<string> GetHtmlContentAsync()
        {
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

            Logger.Warning("Falling back to embedded HTML");
            return HtmlResourceLoader.LoadFallbackHtml(serviceProvider.Options);
        }

        private void SettingsBrowser_OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            LoadingStatusLabel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Triggers the page-side <c>getAndSaveIdeConfig()</c> and awaits the bridge's
        /// save-completion signal (5-second timeout treated as failure). Returns true on
        /// success, false on failure or timeout. The save itself is fire-and-forget from JS
        /// under WebView2, so we rely on <see cref="HtmlSettingsScriptingBridge.SaveCompletion"/>
        /// to surface success / failure.
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                scriptingBridge.BeginSave();
                await host.ExecuteScriptAsync("getAndSaveIdeConfig()");

                var saveTask = scriptingBridge.SaveCompletion;
                var winner = await Task.WhenAny(saveTask, Task.Delay(TimeSpan.FromSeconds(5)));
                if (winner != saveTask)
                {
                    Logger.Warning("Save did not complete within 5s; treating as failure");
                    return false;
                }
                return await saveTask;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to invoke getAndSaveIdeConfig");
                return false;
            }
        }

        /// <summary>
        /// Pushes a freshly-issued token / apiUrl into the still-open settings page so the
        /// visible token field updates after an LS-driven OAuth flow. Called from
        /// <see cref="SnykLanguageClientCustomTarget.OnHasAuthenticated"/>.
        /// </summary>
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            host?.Dispose();
        }
    }
}
