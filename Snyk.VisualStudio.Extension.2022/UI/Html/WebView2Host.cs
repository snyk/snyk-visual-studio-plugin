using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
using Snyk.VisualStudio.Extension.Extension;
#if DEBUG
using Newtonsoft.Json.Linq;
#endif

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Wires a WPF <see cref="WebView2"/> control to the JS↔C# bridge used by the LS-authored
    /// HTML pages: registers the <see cref="WebView2BridgeBindings"/> shim, routes
    /// <c>WebMessageReceived</c> through the supplied <see cref="WebView2MessageDispatcher"/>,
    /// and handles the &gt;2&nbsp;MB <c>NavigateToString</c> spill-to-disk fallback. Hosts
    /// pointing at the same user-data folder share a single <see cref="CoreWebView2Environment"/>
    /// instance (and therefore a single Edge browser process); see remarks for the folder
    /// layout.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Folder layout — two contexts per VS process:
    /// <c>%LOCALAPPDATA%\Snyk\WebView2\&lt;pid&gt;\toolwindow\</c> hosts the description
    /// and summary panels (they live in the same tool window and share an env);
    /// <c>%LOCALAPPDATA%\Snyk\WebView2\&lt;pid&gt;\settings\</c> is dedicated to the modal
    /// settings dialog.
    /// </para>
    /// <para>
    /// The settings dialog uses its own folder to avoid state sharing issues (manifested as
    /// HRESULT 0x8007139F (ERROR_INVALID_STATE)). The description + summary panels can share fine,
    /// so the most likely root cause is a DPI-awareness mismatch between VS's
    /// modal <c>DialogWindow</c> and its tool windows — WebView2 requires every controller
    /// attached to a shared environment to be created in a consistent DPI-awareness
    /// context. See <a href="https://github.com/MicrosoftEdge/WebView2Feedback/issues/2323">
    /// WebView2Feedback#2323</a>.
    /// </para>
    /// </remarks>
    public sealed class WebView2Host : IWebView2Host
    {
        private static readonly ILogger Logger = LogManager.ForContext<WebView2Host>();

        private readonly WebView2 _webView;
        private readonly WebView2MessageDispatcher _dispatcher;
        private readonly WebView2NavigationPreparer _preparer;
        private readonly string _userDataFolder;
        private readonly IReadOnlyList<string> _additionalInitScripts;
        private readonly bool _enableDeveloperTools;

        private bool _initialized;
        private TaskCompletionSource<bool> _readyTcs =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Constructs the host. <paramref name="userDataFolder"/> is the Chromium user-data
        /// folder for this host's environment; use <see cref="BuildUserDataFolder"/> to obtain
        /// a per-panel + per-VS-process path.
        /// <paramref name="additionalInitScripts"/> are registered via
        /// <c>AddScriptToExecuteOnDocumentCreatedAsync</c> after the bridge bindings, before
        /// the first navigation — for example, <see cref="ExecuteCommandBridge.BuildClientScript"/>
        /// which defines <c>window.__ideExecuteCommand__</c> with its callback-id roundtrip.
        /// <paramref name="enableDeveloperTools"/> turns on the Chromium DevTools (F12) for the
        /// hosted control — useful for the debug subclasses, off in production.
        /// </summary>
        public WebView2Host(
            WebView2 webView,
            WebView2MessageDispatcher dispatcher,
            string userDataFolder,
            IEnumerable<string> additionalInitScripts = null,
            bool enableDeveloperTools = false)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            if (string.IsNullOrEmpty(userDataFolder)) throw new ArgumentException("User data folder is required", nameof(userDataFolder));

            _userDataFolder = userDataFolder;
            _additionalInitScripts = (additionalInitScripts ?? Enumerable.Empty<string>()).ToArray();
            _enableDeveloperTools = enableDeveloperTools;

            // Per-host scratch subfolder so two hosts sharing the same user-data folder (e.g. the
            // description + summary panels under "toolwindow") don't sweep each other's oversized-HTML
            // temp files. Orphan subfolders left by a non-disposed host get cleaned up by the per-pid
            // root sweep on the next VS launch.
            var scratchDir = Path.Combine(_userDataFolder, "scratch", Guid.NewGuid().ToString("N"));
            _preparer = new WebView2NavigationPreparer(scratchDir);
        }

        /// <summary>
        /// Completes once <see cref="InitializeAsync"/> has finished and the control is
        /// ready for navigation / script execution. Faults with the init exception on failure;
        /// a subsequent <see cref="InitializeAsync"/> call resets <see cref="Ready"/> for retry.
        /// </summary>
        public Task Ready => _readyTcs.Task;

        /// <summary>
        /// True when the Microsoft Edge WebView2 Runtime is installed and usable. The runtime is a
        /// hard requirement for the extension now that the settings dialog and tool-window panels
        /// are all WebView2-hosted. VS 2022 ships the evergreen runtime, but locked-down or
        /// customized environments can lack it — callers use this to surface an actionable error
        /// instead of failing opaquely on first navigation.
        /// </summary>
        public static bool IsRuntimeAvailable()
        {
            try
            {
                // Throws WebView2RuntimeNotFoundException when no runtime is installed; returns the
                // version string otherwise. Passing null uses the default evergreen runtime lookup.
                var version = CoreWebView2Environment.GetAvailableBrowserVersionString(null);
                return !string.IsNullOrEmpty(version);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "WebView2 runtime availability check failed; treating runtime as not installed");
                return false;
            }
        }

        /// <summary>
        /// Builds a per-context + per-VS-process user-data folder path under
        /// <c>%LOCALAPPDATA%\Snyk\WebView2\&lt;pid&gt;\&lt;contextKey&gt;</c>. Production
        /// uses two keys: <c>"toolwindow"</c> (shared by the description and summary
        /// panels) and <c>"settings"</c> (the modal dialog, see class-level remarks for
        /// why it's isolated). The per-process root is essential because WebView2 takes
        /// an exclusive lock on the user-data folder — two VS instances running
        /// concurrently would otherwise contend. Sibling <c>&lt;pid&gt;</c> folders whose
        /// process has exited are swept on first call so they don't accumulate across
        /// crashed sessions.
        /// </summary>
        public static string BuildUserDataFolder(string contextKey)
        {
            if (string.IsNullOrEmpty(contextKey)) throw new ArgumentException("Context key is required", nameof(contextKey));

            _ = OrphanCleanupOnce.Value;

            var pid = Process.GetCurrentProcess().Id;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Snyk", "WebView2", pid.ToString(), contextKey);
        }

        private static readonly Lazy<bool> OrphanCleanupOnce = new Lazy<bool>(() =>
        {
            TryCleanupOrphanFolders();
            return true;
        });

        // Per-folder environment cache so multiple WebView2 controls sharing the same
        // user-data folder share the same env instance — which is what WebView2 requires
        // for safe shared-folder operation (see WebView2Feedback#2323 + class-level remarks).
        // The share/evict/faulted-recreate logic lives in WebView2EnvironmentCache (unit-tested);
        // here it's wired to the real CoreWebView2Environment.CreateAsync.
        private static readonly WebView2EnvironmentCache<CoreWebView2Environment> EnvironmentCache =
            new WebView2EnvironmentCache<CoreWebView2Environment>(
                userDataFolder => CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    options: null));

        /// <summary>
        /// Drops the cached <see cref="CoreWebView2Environment"/> entry for the given user-data
        /// folder so the next <see cref="InitializeAsync"/> call creates a fresh env (and a
        /// fresh <c>msedgewebview2.exe</c> subprocess). Use after the only host using a folder
        /// has been disposed — keeps the env alive when multiple hosts share a folder (e.g. the
        /// description + summary panels under <c>"toolwindow"</c>) but lets the settings Dialog
        /// start clean each time (as there is no state to preserve).
        /// </summary>
        public static void EvictEnvironmentCache(string userDataFolder) =>
            EnvironmentCache.Evict(userDataFolder);

        private static Task<CoreWebView2Environment> GetOrCreateEnvironmentAsync(string userDataFolder) =>
            EnvironmentCache.GetOrCreate(userDataFolder);

        private static void TryCleanupOrphanFolders()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Snyk", "WebView2");

            if (!Directory.Exists(root)) return;

            var currentPid = Process.GetCurrentProcess().Id;
            foreach (var dir in Directory.GetDirectories(root))
            {
                var name = Path.GetFileName(dir);
                if (!int.TryParse(name, out var pid)) continue;
                if (pid == currentPid) continue;
                if (IsProcessAlive(pid)) continue;

                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to clean up orphan WebView2 folder {Path}", dir);
                }
            }
        }

        private static bool IsProcessAlive(int pid)
        {
            try
            {
                using (var process = Process.GetProcessById(pid))
                {
                    return !process.HasExited;
                }
            }
            catch (ArgumentException)
            {
                // GetProcessById throws when the PID does not refer to a running process.
                return false;
            }
            catch (InvalidOperationException)
            {
                // Process has exited between GetProcessById and the HasExited check.
                return false;
            }
        }

        public async Task InitializeAsync()
        {
            // _initialized gates concurrent calls so the script-registration step doesn't run
            // twice (which would double-fire the bridge bindings on every page load). On failure
            // we roll back the gate and swap in a fresh _readyTcs so a follow-up call can retry
            // rather than awaiting a permanently-faulted Ready task.
            if (_initialized) return;
            _initialized = true;

            var currentTcs = _readyTcs;
            try
            {
                var environment = await GetOrCreateEnvironmentAsync(_userDataFolder);
                await _webView.EnsureCoreWebView2Async(environment);

                ConfigureSettings(_webView.CoreWebView2.Settings, _enableDeveloperTools);

                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    WebView2BridgeBindings.BuildScript());

                foreach (var script in _additionalInitScripts)
                {
                    await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
                }

                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                // Lock the control to the content we load (our own about:/data:/scratch-file
                // documents). LS-served HTML routes real links through window.OpenLink → the OS
                // browser, so any in-control navigation to an off-origin URL is unexpected and is
                // blocked here while the C# bridges stay wired. Popups are never opened in-place.
                _webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
                _webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;

#if DEBUG
                await EnableJsDiagnosticsAsync();
#endif

                currentTcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "WebView2 initialization failed");
                currentTcs.TrySetException(ex);
                _readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _initialized = false;
                throw;
            }
        }

        public async Task NavigateAsync(string html)
        {
            await Ready.ConfigureAwait(true);

            var payload = _preparer.Prepare(html);
            if (payload.IsFile)
            {
                _webView.Source = payload.FileUri;
            }
            else
            {
                _webView.NavigateToString(payload.InlineHtml);
            }
        }

        public async Task<string> ExecuteScriptAsync(string js)
        {
            await Ready.ConfigureAwait(true);
            return await _webView.CoreWebView2.ExecuteScriptAsync(js);
        }

        public void Dispose()
        {
            if (_webView?.CoreWebView2 != null)
            {
                _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                _webView.CoreWebView2.NavigationStarting -= OnNavigationStarting;
                _webView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
            }
            // WebView2.Dispose tears down the underlying msedgewebview2.exe renderer process —
            // without this, every settings-dialog close or tool-window refresh would leak one.
            _webView?.Dispose();
            _preparer.Dispose();
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            _dispatcher.Dispatch(e.WebMessageAsJson);
        }

        // Cancel any top-level navigation that isn't to the content we loaded ourselves
        // (about:/data: from NavigateToString, or a scratch file under our user-data folder).
        // Anchor clicks in LS HTML are intercepted into window.OpenLink, so an off-origin
        // navigation reaching here is a JS redirect / meta-refresh / injected content — not a
        // user action — and is dropped rather than navigated with the bridges still live.
        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (IsAllowedDocumentUri(e.Uri, _userDataFolder)) return;

            Logger.Warning("Blocking WebView2 navigation to disallowed URI: {Uri}", e.Uri);
            e.Cancel = true;
        }

        // Never open a popup window inside the control. Route a genuine http/https target (e.g.
        // target="_blank") to the OS browser; drop anything else.
        private void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            if (UriExtensions.IsValidWebUrl(e.Uri))
            {
                TryOpenExternally(e.Uri);
            }
            else
            {
                Logger.Warning("Blocking WebView2 new-window request for disallowed URI: {Uri}", e.Uri);
            }
        }

        // internal static for testability (InternalsVisibleTo test project): pure allowlist logic
        // with the user-data folder passed in rather than read from instance state.
        internal static bool IsAllowedDocumentUri(string uri, string userDataFolder)
        {
            if (string.IsNullOrEmpty(uri))
                return true; // initial / empty document

            // NavigateToString surfaces as about:blank or a data: document depending on size.
            if (uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase)
                || uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return true;

            // Oversized HTML is spilled to a scratch file and loaded via file://; only allow files
            // under this host's own user-data folder.
            if (uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var path = Path.GetFullPath(new Uri(uri).LocalPath);
                    var root = Path.GetFullPath(userDataFolder).TrimEnd(
                        Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    return path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static void TryOpenExternally(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to open URL in the external browser: {Url}", url);
            }
        }

        private static void ConfigureSettings(CoreWebView2Settings settings, bool enableDeveloperTools)
        {
            settings.AreDefaultContextMenusEnabled = enableDeveloperTools;
            settings.IsStatusBarEnabled = false;
            settings.AreDevToolsEnabled = enableDeveloperTools;
            settings.IsZoomControlEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = false;
        }

#if DEBUG
        // DEBUG-only: route uncaught JS exceptions and console.* calls into Serilog via the
        // Chrome DevTools Protocol. Replaces the IE `wbHandler.ScriptErrorsSuppressed = false`
        // behaviour from the pre-WebView2 debug window — but now visible across every panel
        // and persisted to the Snyk log file rather than a transient popup. Release builds
        // strip this out entirely.
        private async Task EnableJsDiagnosticsAsync()
        {
            try
            {
                await _webView.CoreWebView2.CallDevToolsProtocolMethodAsync("Runtime.enable", "{}");

                var exceptionReceiver = _webView.CoreWebView2
                    .GetDevToolsProtocolEventReceiver("Runtime.exceptionThrown");
                exceptionReceiver.DevToolsProtocolEventReceived += OnJsExceptionThrown;

                var consoleReceiver = _webView.CoreWebView2
                    .GetDevToolsProtocolEventReceiver("Runtime.consoleAPICalled");
                consoleReceiver.DevToolsProtocolEventReceived += OnJsConsoleApiCalled;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to enable WebView2 JS diagnostics");
            }
        }

        private static void OnJsExceptionThrown(object sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
        {
            try
            {
                var parsed = JObject.Parse(e.ParameterObjectAsJson);
                var details = parsed["exceptionDetails"];
                var text = details?["text"]?.Value<string>() ?? "(no text)";
                var url = details?["url"]?.Value<string>() ?? "(no url)";
                var line = details?["lineNumber"]?.Value<int?>();
                var description = details?["exception"]?["description"]?.Value<string>();
                Logger.Error("WebView2 JS exception: {Text} at {Url}:{Line} | {Description}",
                    text, url, line, description ?? "(no detail)");
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to parse WebView2 JS exception payload");
            }
        }

        private static void OnJsConsoleApiCalled(object sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
        {
            try
            {
                var parsed = JObject.Parse(e.ParameterObjectAsJson);
                var type = parsed["type"]?.Value<string>() ?? "log";
                var args = parsed["args"] as JArray;
                var message = args == null
                    ? string.Empty
                    : string.Join(" ", args.Select(FormatConsoleArg));

                switch (type)
                {
                    case "error":
                    case "assert":
                        Logger.Error("WebView2 console.{Type}: {Message}", type, message);
                        break;
                    case "warning":
                        Logger.Warning("WebView2 console.{Type}: {Message}", type, message);
                        break;
                    default:
                        Logger.Information("WebView2 console.{Type}: {Message}", type, message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to parse WebView2 console event payload");
            }
        }

        private static string FormatConsoleArg(JToken arg)
        {
            // Runtime.RemoteObject: prefer `description` (covers Errors, functions, etc.),
            // fall back to the primitive `value`.
            var description = arg?["description"]?.Value<string>();
            if (!string.IsNullOrEmpty(description)) return description;
            var value = arg?["value"];
            return value?.ToString() ?? "(undefined)";
        }
#endif
    }
}
