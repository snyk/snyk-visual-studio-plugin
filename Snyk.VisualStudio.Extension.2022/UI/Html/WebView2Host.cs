using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;

namespace Snyk.VisualStudio.Extension.UI.Html
{
    /// <summary>
    /// Wires a WPF <see cref="WebView2"/> control to the JS↔C# bridge used by the LS-authored
    /// HTML pages: registers the <see cref="WebView2BridgeBindings"/> shim, routes
    /// <c>WebMessageReceived</c> through the supplied <see cref="WebView2MessageDispatcher"/>,
    /// and handles the &gt;2&nbsp;MB <c>NavigateToString</c> spill-to-disk fallback. Each host
    /// owns its own <see cref="CoreWebView2Environment"/> bound to a per-panel user-data folder.
    /// </summary>
    /// <remarks>
    /// We tried sharing a single <see cref="CoreWebView2Environment"/> across all panels in
    /// the process (Microsoft's recommended pattern). Sharing the env between two
    /// <c>WebView2</c> controls hosted in the tool window (the description and summary
    /// panels) worked fine — both controls could be opened simultaneously. But binding the
    /// settings dialog's <c>WebView2</c> to that same env via <c>EnsureCoreWebView2Async</c>
    /// consistently failed with HRESULT 0x8007139F (ERROR_INVALID_STATE). The failure
    /// reproduced regardless of whether settings was opened before or after the tool window.
    /// The asymmetry points at something about Visual Studio's <c>DialogWindow</c> (its
    /// nested message pump, or how it parents the WebView2 HwndHost) interacting badly with
    /// WebView2's env-binding state machine; we never pinned down the precise cause.
    /// Per-panel envs work reliably across all opening orders. Revisit shared envs if the
    /// memory cost becomes a concern.
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
        private readonly TaskCompletionSource<bool> _readyTcs =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _initialized;

        /// <summary>
        /// Constructs the host. <paramref name="userDataFolder"/> is the Chromium user-data
        /// folder for this host's environment; use <see cref="BuildUserDataFolder"/> to obtain
        /// a per-panel + per-VS-process path.
        /// <paramref name="additionalInitScripts"/> are registered via
        /// <c>AddScriptToExecuteOnDocumentCreatedAsync</c> after the bridge bindings, before
        /// the first navigation — for example, <see cref="ExecuteCommandBridge.BuildClientScript"/>
        /// which redefines <c>window.__ideExecuteCommand__</c> with its callback-id roundtrip.
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
            _preparer = new WebView2NavigationPreparer(Path.Combine(_userDataFolder, "scratch"));
        }

        /// <summary>
        /// Completes once <see cref="InitializeAsync"/> has finished and the control is
        /// ready for navigation / script execution. Faults with the init exception on failure.
        /// </summary>
        public Task Ready => _readyTcs.Task;

        /// <summary>
        /// Builds a per-panel + per-VS-process user-data folder path under
        /// <c>%LOCALAPPDATA%\Snyk\WebView2\&lt;pid&gt;\&lt;panelKey&gt;</c>. WebView2 takes
        /// an exclusive lock on the user-data folder, so the per-process root is essential
        /// — two VS instances running concurrently would otherwise contend for the same
        /// folder. Sibling <c>&lt;pid&gt;</c> folders whose process has exited are swept on
        /// first call so they don't accumulate across crashed sessions.
        /// </summary>
        public static string BuildUserDataFolder(string panelKey)
        {
            if (string.IsNullOrEmpty(panelKey)) throw new ArgumentException("Panel key is required", nameof(panelKey));

            _ = OrphanCleanupOnce.Value;

            var pid = Process.GetCurrentProcess().Id;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Snyk", "WebView2", pid.ToString(), panelKey);
        }

        private static readonly Lazy<bool> OrphanCleanupOnce = new Lazy<bool>(() =>
        {
            TryCleanupOrphanFolders();
            return true;
        });

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
            if (_initialized) return;
            _initialized = true;

            try
            {
                var environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: _userDataFolder,
                    options: null);
                await _webView.EnsureCoreWebView2Async(environment);

                ConfigureSettings(_webView.CoreWebView2.Settings, _enableDeveloperTools);

                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    WebView2BridgeBindings.BuildScript());

                foreach (var script in _additionalInitScripts)
                {
                    await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
                }

                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                _readyTcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "WebView2 initialization failed");
                _readyTcs.TrySetException(ex);
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
    }
}
