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

        // Latest LS-issued auth token awaiting delivery to the settings page. OnHasAuthenticated can
        // fire when no control is loaded (between Unloaded and the next show) or before the page's
        // setAuthToken JS exists; queuing here makes delivery at-least-once — whichever control next
        // becomes page-ready flushes it, instead of the push being silently lost. The take-once /
        // last-write-wins semantics live in PendingAuthTokenSlot (unit-tested); this control just
        // gates the flush on page-readiness.
        private static readonly PendingAuthTokenSlot AuthTokenSlot = new PendingAuthTokenSlot();

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

            // Paint the WebView2 surface white before its first render so the dialog doesn't
            // flash black while the Edge process spins up. The settings page is always rendered
            // in light mode, so white matches the loaded content. Must be set before CoreWebView2
            // initialization to take effect on first show.
            SettingsBrowser.DefaultBackgroundColor = System.Drawing.Color.White;

            Logger.Information("[lifecycle] HtmlSettingsControl ctor; instance={Hash}", GetHashCode());

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

            // Trigger the heavy WebView2 init + LS fetch from IsVisibleChanged rather than
            // Loaded. When Tools→Options opens the Snyk page for the second time, VS's
            // hosting layer briefly cycles the control through Loaded → Unloaded → Loaded
            // as it re-arranges its WPF tree under a fresh ElementHost. The intermediate
            // unparent destroys the WebView2's underlying HWND, disposing the wrapper —
            // and any in-flight NavigateAsync started from the first Loaded then throws
            // ObjectDisposedException. IsVisibleChanged only fires once the tree is
            // stable and the control is actually visible, so the init runs after the
            // re-arrangement settles.
            IsVisibleChanged += Control_IsVisibleChanged;
        }

        private bool _initStarted;

        // Set once the hosted page has finished navigating, i.e. once window.setAuthToken exists.
        // A queued auth token is only flushed after this so the push can't no-op against a
        // half-loaded page.
        private volatile bool _pageReady;

        /// <summary>
        /// Set to true after this control has been Unloaded. The host's WebView2 may already
        /// have been disposed by WPF's HwndHost cleanup at that point, so the parent
        /// <see cref="HtmlSettingsDialogPage"/> uses this to know when to swap in a fresh
        /// control instance.
        /// </summary>
        public bool IsStale { get; private set; }

        private async void Control_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_disposed) return;
            Logger.Information("[lifecycle] HtmlSettingsControl IsVisibleChanged; instance={Hash}, IsVisible={Visible}, initStarted={Started}",
                GetHashCode(), IsVisible, _initStarted);
            if (!IsVisible) return;
            if (_initStarted) return;
            _initStarted = true;

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
            Logger.Information("[lifecycle] HtmlSettingsControl Unloaded; instance={Hash}", GetHashCode());
            // After Unloaded the WebView2's underlying CoreWebView2Controller is destroyed by
            // WPF's HwndHost cleanup, leaving the C# wrapper marked disposed. Signal stale so
            // the DialogPage swaps in a fresh instance on the next show.
            IsStale = true;

            // Clear the singleton if we're the currently-tracked control — guards against
            // older instances racing the latest IsVisibleChanged write.
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

                // Settings dialog is always rendered in light mode regardless of VS theme;
                // the tool-window panels (description, summary) keep theme-following.
                html = HtmlResourceLoader.ApplyTheme(html, forceLight: true);
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
                return HtmlResourceLoader.LoadFallbackHtml(serviceProvider.Options, forceLight: true);
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
            return HtmlResourceLoader.LoadFallbackHtml(serviceProvider.Options, forceLight: true);
        }

        private void SettingsBrowser_OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_disposed) return;
            // Reveal the browser only now that the document has loaded, so the user never sees the
            // dark WebView2 surface before the (light-mode) HTML has painted. The white host Grid
            // and loading label covered the area until this point.
            LoadingStatusLabel.Visibility = Visibility.Collapsed;
            SettingsBrowser.Visibility = Visibility.Visible;

            // The page (and its window.setAuthToken) now exists — deliver any auth token that was
            // queued before this control finished loading (e.g. an OAuth round-trip that completed
            // while no settings page was open).
            _pageReady = true;
            FlushPendingAuthToken();
        }

        // Backstop for a *lost* WebView2 round-trip, NOT a Language Server timeout. SaveCompletion
        // is signalled once the bridge has applied the settings to Options and written solution
        // storage — all local/synchronous work. The LS push (DidChangeConfiguration) happens after,
        // fire-and-forget via the SettingsChanged event, and is never awaited here, so a slow LS
        // can't trip this. 5s is generous relative to the JS-injection + message-dispatch + local
        // save it actually covers; it only fires if getAndSaveIdeConfig()/the bridge never reply.
        private static readonly TimeSpan SaveCompletionTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Triggers the page-side <c>getAndSaveIdeConfig()</c> and awaits the bridge's
        /// save-completion signal (<see cref="SaveCompletionTimeout"/> treated as failure). Returns
        /// true on success, false on failure or timeout. The save itself is fire-and-forget from JS
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
                var winner = await Task.WhenAny(saveTask, Task.Delay(SaveCompletionTimeout));
                if (winner != saveTask)
                {
                    Logger.Warning("Save did not complete within {Timeout}; treating as failure", SaveCompletionTimeout);
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
        /// Records the latest LS-issued token / apiUrl and delivers it to the live settings page if
        /// one is ready. Safe to call when no settings page is open, or before its HTML has loaded —
        /// the token is held and flushed by the next control to become page-ready, so a push is never
        /// lost. Called from <see cref="SnykLanguageClientCustomTarget.OnHasAuthenticated"/>.
        /// </summary>
        public static void QueueAuthToken(string token, string apiUrl = null)
        {
            AuthTokenSlot.Set(token, apiUrl);
            instance?.FlushPendingAuthToken();
        }

        /// <summary>
        /// Re-fetches HTML from the LS and re-renders the settings page if one is currently open.
        /// Safe to call from any thread; fire-and-forget. No-op when no settings page is open.
        /// Called by <see cref="SnykLanguageClientCustomTarget.OnSnykConfiguration"/> after settings are applied.
        /// </summary>
        public static void RequestReload()
        {
            var currentInstance = instance;
            if (currentInstance == null || currentInstance._disposed || !currentInstance._pageReady) return;

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (currentInstance._disposed) return;
                await currentInstance.LoadHtmlSettingsAsync();
            }).FireAndForget();
        }

        // Delivers the queued token to the page, but only once it is loaded (window.setAuthToken
        // exists); otherwise it is left queued for SettingsBrowser_OnNavigationCompleted to flush.
        // Take() returns the token at most once, so it can't be delivered twice.
        private void FlushPendingAuthToken()
        {
            if (!_pageReady) return;

            var pending = AuthTokenSlot.Take();
            if (pending != null)
            {
                UpdateAuthToken(pending.Token, pending.ApiUrl);
            }
        }

        /// <summary>
        /// Pushes a freshly-issued token / apiUrl into the still-open settings page so the
        /// visible token field updates after an LS-driven OAuth flow.
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

            // Unsubscribe so a late WPF/WebView2 event can't fire on the now-disposed control — that
            // would touch the disposed host/browser and throw ObjectDisposedException inside the
            // handlers (swallowed by their FireAndForget), and the live handlers would otherwise keep
            // this control rooted.
            IsVisibleChanged -= Control_IsVisibleChanged;
            Unloaded -= Control_Unloaded;
            SettingsBrowser.NavigationCompleted -= SettingsBrowser_OnNavigationCompleted;

            host?.Dispose();
        }
    }
}
