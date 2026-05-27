using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
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
    public sealed class WebView2Host : IDisposable
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
        // The in-flight task is cached so concurrent callers join the same CreateAsync rather
        // than racing on the exclusive folder lock; faulted/canceled entries are evicted on
        // next access so a transient init failure doesn't permanently poison the slot.
        private static readonly object EnvironmentGate = new object();
        private static readonly Dictionary<string, Task<CoreWebView2Environment>> Environments =
            new Dictionary<string, Task<CoreWebView2Environment>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Drops the cached <see cref="CoreWebView2Environment"/> entry for the given user-data
        /// folder so the next <see cref="InitializeAsync"/> call creates a fresh env (and a
        /// fresh <c>msedgewebview2.exe</c> subprocess). Use after the only host using a folder
        /// has been disposed — keeps the env alive when multiple hosts share a folder (e.g. the
        /// description + summary panels under <c>"toolwindow"</c>) but lets single-user
        /// surfaces (e.g. the settings DialogPage) start clean each time.
        /// </summary>
        public static void EvictEnvironmentCache(string userDataFolder)
        {
            if (string.IsNullOrEmpty(userDataFolder)) return;
            lock (EnvironmentGate)
            {
                Environments.Remove(userDataFolder);
            }
        }

        private static Task<CoreWebView2Environment> GetOrCreateEnvironmentAsync(string userDataFolder)
        {
            lock (EnvironmentGate)
            {
                if (Environments.TryGetValue(userDataFolder, out var existing))
                {
                    if (!existing.IsFaulted && !existing.IsCanceled) return existing;
                    Environments.Remove(userDataFolder);
                }

                var task = CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    options: null);
                Environments[userDataFolder] = task;
                return task;
            }
        }

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
